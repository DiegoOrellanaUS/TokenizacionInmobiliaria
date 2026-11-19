using Adminstracion.Data;
using Adminstracion.Dominio;
using Adminstracion.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[Route("api/[controller]")]
[ApiController]
public class RolesController(AdminContext context, IWebHostEnvironment environment) : ControllerBase
{
    private const string TitularTenant = "TITULAR_TENANT";

    [HttpGet("Listar", Name = "ListarRoles")]
    [HttpGet("ListarReal", Name = "ListarRealRoles")]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var roles = await (
            from r in context.Rol.AsNoTracking()
            where r.estado.ToUpper() != "BORRADO" && r.estado.ToUpper() != "ELIMINADO"
            orderby r.orden_visual ?? int.MaxValue, r.nombre_visible, r.codigo
            select r
        ).ToListAsync(ct);
        return Ok(roles);
    }

    [HttpGet("Buscar/{id:guid}", Name = "BuscarRol")]
    public async Task<IActionResult> GetRol(Guid id, CancellationToken ct)
    {
        var rol = await (
            from r in context.Rol.AsNoTracking()
            where r.id == id && r.estado.ToUpper() != "BORRADO" && r.estado.ToUpper() != "ELIMINADO"
            select r
        ).FirstOrDefaultAsync(ct);
        if (rol == null)
            return NotFound("El rol no fue encontrado.");
        return Ok(rol);
    }

    [HttpPost("crear", Name = "CrearRol")]
    public async Task<IActionResult> PostRol(CrearRol registro, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var codigo = registro.Codigo.Trim().ToUpperInvariant();
        var nombre = registro.NombreVisible.Trim();
        if (codigo.Length == 0 || nombre.Length == 0)
            return BadRequest("Indica el código y el nombre visible.");
        if (codigo == TitularTenant)
            return Conflict("El rol TITULAR_TENANT lo administra el flujo de suscripción.");

        var rol = new Rol
        {
            codigo = codigo,
            nombre_visible = nombre,
            descripcion = registro.Descripcion?.Trim(),
            orden_visual = registro.OrdenVisual
        };
        context.Rol.Add(rol);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict("El código del rol ya está registrado.");
        }

        return CreatedAtAction(nameof(GetRol), new { id = rol.id }, rol);
    }

    [HttpPut("Actualizar/{id:guid}", Name = "ActualizarRol")]
    public async Task<IActionResult> PutRol(Guid id, EditarRol registro, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var nombre = registro.NombreVisible.Trim();
        var estado = registro.Estado?.Trim().ToUpperInvariant();
        if (nombre.Length == 0)
            return BadRequest("Indica el nombre visible.");
        if (estado != null && estado is not ("ACTIVO" or "INACTIVO"))
            return BadRequest("El estado debe ser ACTIVO o INACTIVO. Usa DELETE para la baja.");

        var consulta =
            from r in context.Rol
            where r.id == id && r.estado.ToUpper() != "BORRADO" && r.estado.ToUpper() != "ELIMINADO"
            select r;
        var codigo = await (
            from r in consulta
            select r.codigo
        ).FirstOrDefaultAsync(ct);
        if (codigo == null)
            return NotFound("El rol no fue encontrado.");
        if (codigo == TitularTenant && estado == "INACTIVO")
            return Conflict("El rol TITULAR_TENANT forma parte del aprovisionamiento.");

        var descripcion = registro.Descripcion?.Trim();
        var modificados = await consulta.ExecuteUpdateAsync(r => r
            .SetProperty(x => x.nombre_visible, nombre)
            .SetProperty(x => x.descripcion, descripcion)
            .SetProperty(x => x.orden_visual, registro.OrdenVisual)
            .SetProperty(x => x.estado, x => estado ?? x.estado), ct);
        if (modificados == 0)
            return NotFound("El rol no fue encontrado.");

        return NoContent();
    }

    [HttpDelete("Borrar/{id:guid}", Name = "BorrarRol")]
    public async Task<IActionResult> DeleteRol(Guid id, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var consulta =
            from r in context.Rol
            where r.id == id
            select r;
        var codigo = await (
            from r in consulta
            select r.codigo
        ).FirstOrDefaultAsync(ct);
        if (codigo == null)
            return NotFound("El rol no fue encontrado.");
        if (codigo == TitularTenant)
            return Conflict("El rol TITULAR_TENANT forma parte del aprovisionamiento.");

        await (
            from r in consulta
            where r.estado.ToUpper() != "BORRADO" && r.estado.ToUpper() != "ELIMINADO"
            select r
        ).ExecuteUpdateAsync(r => r.SetProperty(x => x.estado, "Borrado"), ct);
        return NoContent();
    }
}
