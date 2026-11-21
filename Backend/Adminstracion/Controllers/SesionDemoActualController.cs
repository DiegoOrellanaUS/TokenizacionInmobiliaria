using Adminstracion.Data;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Controllers;

[ApiController]
[Route("api/SesionDemo/Actual")]
public sealed class SesionDemoActualController(AdminContext context, SesionDemo sesiones) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Actual(CancellationToken ct)
    {
        if (!sesiones.Disponible(HttpContext))
            return NotFound();
        if (!sesiones.Leer(HttpContext, out var usuarioId))
            return Unauthorized();

        var activo = await (from u in context.Usuario.AsNoTracking()
                            where u.id == usuarioId && u.estado == "ACTIVO"
                            select u.id).AnyAsync(ct);
        if (!activo)
            return Unauthorized();

        var tenantId = await (from t in context.Tenant.AsNoTracking()
                              where t.propietario_id == usuarioId
                              select (Guid?)t.id).SingleOrDefaultAsync(ct);
        return Ok(new { UsuarioId = usuarioId, TenantId = tenantId });
    }
}
