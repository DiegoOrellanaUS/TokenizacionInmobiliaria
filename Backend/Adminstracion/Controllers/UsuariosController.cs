
using Adminstracion.Data;
using Microsoft.AspNetCore.RateLimiting;
using Adminstracion.Dominio;
using Adminstracion.DTOs;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[Route("api/[controller]")]
[ApiController]
public class UsuariosController(AdminContext context, AprovisionarTenant aprovisionador,
    IPasswordHasher<Usuario> passwordHasher, IWebHostEnvironment environment, SesionDemo sesiones,
    InvitacionesRevision invitaciones) : ControllerBase
{
    private IQueryable<Usuario> Usuarios() => context.Usuario.AsNoTracking();

    private async Task<object> DatosUsuario(Usuario usuario, CancellationToken ct)
    {
        var tenant = await (
            from t in context.Tenant.AsNoTracking()
            where t.propietario_id == usuario.id
            select t
        ).FirstOrDefaultAsync(ct);
        return new
        {
            UsuarioId = usuario.id,
            CI = usuario.ci,
            Nombre = string.Join(" ", new[] { usuario.nombres, usuario.apellidos }.Where(n => n.Length > 0)),
            Nombres = usuario.nombres,
            Apellidos = usuario.apellidos,
            CorreoElectronico = usuario.correo_electronico,
            Estado = usuario.estado,
            TenantId = tenant?.id,
            EstadoBaseDatos = tenant == null ? "SIN_TENANT" : tenant.estado == "ACTIVO" ? "LISTA" : tenant.estado,
            Clientes = tenant == null ? Array.Empty<Guid>() : new[] { tenant.id }
        };
    }

    [HttpGet("Listar", Name = "ListarUsuarios")]
    [HttpGet("ListarReal", Name = "ListarRealUsuarios")]
    public async Task<IActionResult> GetUsuario(CancellationToken ct)
    {
        var usuarios = await (
            from u in Usuarios()
            where u.estado.ToUpper() != "BORRADO" && u.estado.ToUpper() != "ELIMINADO"
            join t in context.Tenant on u.id equals t.propietario_id into tenants
            from tenant in tenants.DefaultIfEmpty()
            orderby u.nombres, u.apellidos, u.id
            select new
            {
                UsuarioId = u.id, CI = u.ci, Nombre = u.apellidos == "" ? u.nombres : u.nombres + " " + u.apellidos,
                Nombres = u.nombres, Apellidos = u.apellidos, CorreoElectronico = u.correo_electronico,
                Estado = u.estado, TenantId = tenant == null ? (Guid?)null : tenant.id,
                EstadoBaseDatos = tenant == null ? "SIN_TENANT" : tenant.estado == "ACTIVO" ? "LISTA" : tenant.estado
            }).ToListAsync(ct);
        return Ok(usuarios);
    }

    [HttpGet("Buscar/{usuarioid:guid}", Name = "BuscarUsuario")]
    public async Task<IActionResult> GetUsuario(Guid usuarioid, CancellationToken ct)
    {
        var usuario = await (
            from u in Usuarios()
            where u.id == usuarioid && u.estado.ToUpper() != "BORRADO" && u.estado.ToUpper() != "ELIMINADO"
            select u
        ).FirstOrDefaultAsync(ct);
        if (usuario == null)
            return NotFound("El usuario no fue encontrado.");
        if (usuario.estado != "ACTIVO")
            return BadRequest("El usuario se encuentra inactivo.");
        return Ok(await DatosUsuario(usuario, ct));
    }

    [HttpPost("crear", Name = "CrearUsuario")]
    [EnableRateLimiting("cuentas")]
    public async Task<IActionResult> PostUsuario(RegistroUsuario registro, CancellationToken ct)
    {
        if (sesiones.SoloOAuth) return NotFound();
        var codigo = registro.CodigoRevisor?.Trim();
        if (registro.CodigoRevisor is not null && string.IsNullOrEmpty(codigo))
            return BadRequest(InvitacionesRevision.MensajeInvalida);
        if (codigo is not null && !sesiones.Disponible(HttpContext)) return NotFound();
        if ((environment.IsEnvironment("Piloto") || !string.IsNullOrEmpty(codigo)) && registro.Contrasenia.Length < 12)
            return BadRequest("Usa una contraseña de al menos 12 caracteres.");
        var ci = registro.CI.Trim();
        var nombre = registro.Nombre.Trim();
        var correo = registro.CorreoElectronico.Trim().ToLowerInvariant();
        if (ci.Length == 0 || nombre.Length == 0 || string.IsNullOrWhiteSpace(registro.Contrasenia))
            return BadRequest("Completa el CI, nombre y contraseña.");
        var existeUsuario = await (
            from u in Usuarios()
            where u.ci == ci || u.correo_electronico == correo
            select u.id
        ).AnyAsync(ct);
        if (existeUsuario)
            return Conflict("Ya existe un usuario con ese CI o correo electrónico.");

        var usuario = new Usuario
        {
            id = Guid.NewGuid(),
            nombres = nombre,
            apellidos = registro.Apellidos?.Trim() ?? "",
            ci = ci,
            correo_electronico = correo
        };
        usuario.contrasenia_hash = passwordHasher.HashPassword(usuario, registro.Contrasenia);
        context.Usuario.Add(usuario);
        await using var tx = string.IsNullOrEmpty(codigo) ? null : await context.Database.BeginTransactionAsync(ct);
        try
        {
            if (tx is not null && !await invitaciones.PrepararCanjeAsync(codigo!, usuario, ct))
                return BadRequest(InvitacionesRevision.MensajeInvalida);

            await context.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict("Ya existe un usuario con ese CI o correo electrónico.");
        }

        return CreatedAtAction(nameof(GetUsuario), new { usuarioid = usuario.id },
            await DatosUsuario(usuario, ct));
    }

    [NonAction]
    public Task PrepararBasesTenant(Guid tenantId, params string[] servicios) =>
        aprovisionador.PrepararBasesTenant(tenantId, servicios, CancellationToken.None);
}
