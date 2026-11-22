using Adminstracion.Data;
using Adminstracion.Dominio;
using Adminstracion.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[Route("api/[controller]")]
[ApiController]
public class UsuarioRolController(AdminContext context, IWebHostEnvironment environment) : ControllerBase
{
    private const string TitularTenant = "TITULAR_TENANT";

    private static IQueryable<UsuarioRolDetalle> ProyectarAsignaciones(IQueryable<UsuarioRol> consulta) =>
        from asignacion in consulta
        select new UsuarioRolDetalle(
            asignacion.id, asignacion.usuario_id, asignacion.rol_id,
            asignacion.Rol.codigo, asignacion.Rol.estado, asignacion.alcance,
            asignacion.tenant_id, asignacion.token_ref,
            asignacion.fecha_creacion, asignacion.fecha_eliminacion);

    [HttpGet("Listar", Name = "ListarUsuarioRol")]
    [HttpGet("ListarReal", Name = "ListarRealUsuarioRol")]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var consulta =
            from r in context.UsuarioRol.AsNoTracking()
            where r.fecha_eliminacion == null
            orderby r.fecha_creacion descending, r.id
            select r;
        var roles = await ProyectarAsignaciones(consulta).ToListAsync(ct);
        return Ok(roles);
    }

    [HttpGet("Historial/{usuarioId:guid}", Name = "HistorialUsuarioRol")]
    public async Task<IActionResult> GetHistorial(Guid usuarioId, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var consulta =
            from r in context.UsuarioRol.AsNoTracking()
            where r.usuario_id == usuarioId
            orderby r.fecha_creacion descending, r.id
            select r;
        var historial = await ProyectarAsignaciones(consulta).ToListAsync(ct);
        return Ok(historial);
    }

    [HttpGet("{usuarioId:guid}/Buscar/{id:guid}", Name = "BuscarAsignacionRol")]
    public async Task<IActionResult> GetAsignacion(Guid usuarioId, Guid id, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var consulta =
            from r in context.UsuarioRol.AsNoTracking()
            where r.usuario_id == usuarioId && r.id == id
            select r;
        var asignacion = await ProyectarAsignaciones(consulta).FirstOrDefaultAsync(ct);
        if (asignacion == null)
            return NotFound("La asignación no fue encontrada.");
        return Ok(asignacion);
    }

    [HttpPost("{usuarioId:guid}/asignar", Name = "AsignarRolUsuario")]
    public async Task<IActionResult> PostAsignacion(Guid usuarioId, AsignarRol registro, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        if (usuarioId == Guid.Empty || registro.RolId == Guid.Empty ||
            registro.TenantId == Guid.Empty || registro.TokenRef == Guid.Empty)
            return BadRequest("Los identificadores deben ser válidos.");
        if (registro.TokenRef.HasValue && !registro.TenantId.HasValue)
            return BadRequest("Indica el tenant propietario del token.");

        var estadoUsuario = await (
            from u in context.Usuario.AsNoTracking()
            where u.id == usuarioId
            select u.estado
        ).FirstOrDefaultAsync(ct);
        if (estadoUsuario == null)
            return NotFound("El usuario no fue encontrado.");
        if (estadoUsuario != "ACTIVO")
            return Conflict("El usuario debe estar activo.");

        var rol = await (
            from r in context.Rol.AsNoTracking()
            where r.id == registro.RolId
            select r
        ).FirstOrDefaultAsync(ct);
        if (rol == null)
            return NotFound("El rol no fue encontrado.");
        if (rol.estado != "ACTIVO" || rol.codigo == TitularTenant)
            return Conflict("El rol seleccionado no está disponible para asignación manual.");

        var alcance = registro.TokenRef.HasValue ? "TOKEN" : registro.TenantId.HasValue ? "TENANT" : "GLOBAL";
        if (rol.codigo == "SUPER_ADMIN")
            return StatusCode(403, "El rol SUPER_ADMIN se concede por administración de la base de datos.");
        if (rol.codigo is ("ADMIN_TOKEN" or "CLIENTE_TOKEN" or "ACCIONISTA_TOKEN") && alcance != "TOKEN")
            return BadRequest("Este rol requiere un tenant y una referencia de token.");

        if (registro.TenantId.HasValue)
        {
            var tenantActivo = await (
                from t in context.Tenant.AsNoTracking()
                where t.id == registro.TenantId && t.estado == "ACTIVO"
                select t.id
            ).AnyAsync(ct);
            if (!tenantActivo)
                return BadRequest("El tenant indicado debe existir y estar activo.");
        }

        var asignacion = new UsuarioRol
        {
            usuario_id = usuarioId,
            rol_id = rol.id,
            alcance = alcance,
            tenant_id = registro.TenantId,
            token_ref = registro.TokenRef
        };
        context.UsuarioRol.Add(asignacion);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict("El usuario ya tiene una asignación vigente de ese rol en el mismo alcance.");
        }

        return CreatedAtAction(nameof(GetAsignacion), new { usuarioId, id = asignacion.id },
            new UsuarioRolDetalle(asignacion.id, usuarioId, rol.id, rol.codigo, rol.estado, alcance,
                asignacion.tenant_id, asignacion.token_ref, asignacion.fecha_creacion, asignacion.fecha_eliminacion));
    }

    [HttpDelete("{usuarioId:guid}/Borrar/{id:guid}", Name = "BorrarAsignacionRol")]
    public async Task<IActionResult> DeleteAsignacion(Guid usuarioId, Guid id, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var consulta =
            from r in context.UsuarioRol
            where r.usuario_id == usuarioId && r.id == id
            select r;
        var codigo = await (
            from r in consulta
            select r.Rol.codigo
        ).FirstOrDefaultAsync(ct);
        if (codigo == null)
            return NotFound("La asignación no fue encontrada.");
        if (codigo == "SUPER_ADMIN")
            return StatusCode(403, "El rol SUPER_ADMIN se revoca por administración de la base de datos.");
        if (codigo == TitularTenant)
            return Conflict("El titular del tenant se gestiona en el flujo de suscripción.");

        var ahora = DateTimeOffset.UtcNow;
        await (
            from r in consulta
            where r.fecha_eliminacion == null
            select r
        ).ExecuteUpdateAsync(r => r.SetProperty(x => x.fecha_eliminacion,
            x => x.fecha_creacion > ahora ? x.fecha_creacion : ahora), ct);
        return NoContent();
    }
}
