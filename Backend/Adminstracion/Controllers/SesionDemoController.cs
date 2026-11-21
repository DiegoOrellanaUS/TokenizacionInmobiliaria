using System.ComponentModel.DataAnnotations;
using Adminstracion.Data;
using Adminstracion.Dominio;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Controllers;

[ApiController]
[Route("api/SesionDemo")]
public sealed class SesionDemoController(AdminContext context, SesionDemo sesiones, IConfiguration configuration,
    IWebHostEnvironment environment, IPasswordHasher<Usuario> hasher) : ControllerBase
{
    [HttpPost("Iniciar", Name = "IniciarSesionDemo")]
    [EnableRateLimiting("cuentas")]
    public async Task<IActionResult> Iniciar(CorreoSesion solicitud, CancellationToken ct)
    {
        if (!sesiones.Disponible(HttpContext))
            return NotFound();

        var correo = solicitud.Correo.Trim().ToLowerInvariant();
        var usuario = await (from u in context.Usuario.AsNoTracking()
                             where u.correo_electronico == correo && u.estado == "ACTIVO"
                             select u).SingleOrDefaultAsync(ct);
        if (environment.IsEnvironment("Piloto") &&
            (usuario is null || usuario.contrasenia_hash is not { } hash || solicitud.Contrasenia is not { Length: >= 12 } ||
             hasher.VerifyHashedPassword(usuario, hash, solicitud.Contrasenia) == PasswordVerificationResult.Failed))
            return Unauthorized("Correo o contraseña incorrectos.");
        if (usuario is null)
            return NotFound("No encontramos un usuario activo con ese correo.");

        var revisor = await EsRevisor(usuario.id, ct);
        var tenant = await (from t in context.Tenant.AsNoTracking()
                            where t.propietario_id == usuario.id && t.estado == "ACTIVO"
                            select (Guid?)t.id).SingleOrDefaultAsync(ct);

        return Ok(new { UsuarioId = usuario.id, Nombre = usuario.nombres, Correo = correo,
            TenantId = tenant, EsRevisor = revisor, Token = sesiones.Emitir(usuario.id) });
    }

    [HttpGet("EspaciosRevision", Name = "ListarEspaciosRevision")]
    public async Task<IActionResult> ListarEspaciosRevision(CancellationToken ct)
    {
        if (!sesiones.Disponible(HttpContext))
            return NotFound();
        if (!sesiones.Leer(HttpContext, out var usuarioId))
            return Unauthorized();
        if (!await EsRevisor(usuarioId, ct))
            return StatusCode(StatusCodes.Status403Forbidden);

        var ahora = DateTimeOffset.UtcNow;
        var espacios = await (from t in context.Tenant.AsNoTracking()
                              where t.estado == "ACTIVO" && t.Propietario.estado == "ACTIVO" &&
                                  t.propietario_id != usuarioId &&
                                  context.Suscripcion.Any(s => s.tenant_id == t.id && s.usuario_id == t.propietario_id &&
                                      s.estado == "ACTIVA" && s.desde <= ahora && (s.hasta == null || s.hasta > ahora))
                              orderby t.nombre_comercial ?? t.Propietario.nombres, t.id
                              select new { TenantId = t.id, Nombre = t.nombre_comercial ?? t.Propietario.nombres })
                              .ToListAsync(ct);
        return Ok(espacios);
    }

    private async Task<bool> EsRevisor(Guid usuarioId, CancellationToken ct)
    {
        var autorizado = configuration.GetSection("Inmuebles:Revisores").Get<string[]>()?
            .Any(valor => Guid.TryParse(valor, out var id) && id == usuarioId) == true;
        return autorizado && await (from r in context.UsuarioRol.AsNoTracking()
            where r.usuario_id == usuarioId && r.Usuario.estado == "ACTIVO" && r.fecha_eliminacion == null &&
                r.alcance == "GLOBAL" && r.Rol.estado == "ACTIVO" &&
                (r.Rol.codigo == "REVISOR_INMUEBLE" || r.Rol.codigo == "SUPER_ADMIN")
            select r.id).AnyAsync(ct);
    }

    public sealed record CorreoSesion([Required, EmailAddress, StringLength(254)] string Correo,
        [StringLength(128)] string? Contrasenia = null);
}
