using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Adminstracion.Data;
using Adminstracion.Dominio;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/inmuebles")]
public class AccesoInmueblesController(
    AdminContext context, IPasswordHasher<Usuario> hasher, IConfiguration configuration, SesionDemo sesiones) : ControllerBase
{
    [HttpGet("{tenantId:guid}/acceso")]
    public async Task<IActionResult> GetAcceso(Guid tenantId, CancellationToken ct)
    {
        var clave = configuration["Inmuebles:ClaveInterna"];
        if (string.IsNullOrWhiteSpace(clave) ||
            !Request.Headers.TryGetValue("X-Inmuebles-Interna", out var recibida) || recibida.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString())))
            return NotFound();

        var esSesionDemo = sesiones.Leer(HttpContext, out var usuarioId);
        var password = string.Empty;
        if (!esSesionDemo && !LeerCredenciales(Request.Headers.Authorization.ToString(), out usuarioId, out password))
            return Unauthorized();

        var usuario = await (from u in context.Usuario.AsNoTracking()
                             where u.id == usuarioId && u.estado == "ACTIVO"
                             select u).FirstOrDefaultAsync(ct);
        if (usuario is null || (!esSesionDemo && (usuario.contrasenia_hash is not { } hash ||
            hasher.VerifyHashedPassword(usuario, hash, password) == PasswordVerificationResult.Failed)))
            return Unauthorized();

        var ahora = DateTimeOffset.UtcNow;
        var tenant = await (from t in context.Tenant.AsNoTracking()
                            where t.id == tenantId && t.estado == "ACTIVO" && t.Propietario.estado == "ACTIVO"
                            select new { t.propietario_id }).FirstOrDefaultAsync(ct);
        if (tenant is null)
            return StatusCode(StatusCodes.Status403Forbidden);

        var condiciones = await (from s in context.Suscripcion.AsNoTracking()
                                 where s.tenant_id == tenantId && s.usuario_id == tenant.propietario_id &&
                                       s.estado == "ACTIVA" && s.desde <= ahora && (s.hasta == null || s.hasta > ahora)
                                 orderby s.desde descending
                                 select s.condiciones_contratadas).FirstOrDefaultAsync(ct);
        if (condiciones is null)
            return StatusCode(StatusCodes.Status403Forbidden);

        var roles = from r in context.UsuarioRol.AsNoTracking()
                    where r.usuario_id == usuarioId && r.fecha_eliminacion == null && r.Rol.estado == "ACTIVO"
                    select r;

        var esTitular = tenant.propietario_id == usuarioId &&
            await (from r in roles
                   where r.tenant_id == tenantId && r.alcance == "TENANT" && r.Rol.codigo == "TITULAR_TENANT"
                   select r.id).AnyAsync(ct);

        // Los revisores se habilitan por configuración, además de su rol.
        var autorizadoComoRevisor = configuration.GetSection("Inmuebles:Revisores").Get<string[]>()?
            .Any(valor => Guid.TryParse(valor, out var id) && id == usuarioId) == true;
        var esRevisor = tenant.propietario_id != usuarioId && autorizadoComoRevisor &&
            await (from r in roles
                   where r.alcance == "GLOBAL" && (r.Rol.codigo == "REVISOR_INMUEBLE" || r.Rol.codigo == "SUPER_ADMIN")
                   select r.id).AnyAsync(ct);

        if (!esTitular && !esRevisor)
            return StatusCode(StatusCodes.Status403Forbidden);

        using var planContratado = JsonDocument.Parse(condiciones);
        var maxTokensPropios = planContratado.RootElement.ValueKind == JsonValueKind.Object &&
            planContratado.RootElement.TryGetProperty("max_tokens_propios", out var cupo) &&
            cupo.ValueKind == JsonValueKind.Number && cupo.TryGetInt32(out var maximo) ? maximo : 0;

        return Ok(new { UsuarioId = usuarioId, EsTitular = esTitular, EsRevisor = esRevisor, MaxTokensPropios = maxTokensPropios });
    }

    private static bool LeerCredenciales(string valor, out Guid usuarioId, out string password)
    {
        usuarioId = Guid.Empty;
        password = string.Empty;
        if (valor.Length > 4096 || !AuthenticationHeaderValue.TryParse(valor, out var header) ||
            !header.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase) ||
            header.Parameter is not { } parametro)
            return false;

        string credenciales;
        try
        {
            credenciales = Encoding.UTF8.GetString(Convert.FromBase64String(parametro));
        }
        catch (FormatException)
        {
            return false;
        }

        var separador = credenciales.IndexOf(':');
        if (separador <= 0 || !Guid.TryParse(credenciales.AsSpan(0, separador), out usuarioId) || usuarioId == Guid.Empty)
            return false;

        password = credenciales[(separador + 1)..];
        return password.Length is > 0 and <= 1024;
    }
}
