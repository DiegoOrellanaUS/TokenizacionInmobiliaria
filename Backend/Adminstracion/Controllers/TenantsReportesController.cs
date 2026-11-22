using System.Security.Cryptography;
using System.Text;
using Adminstracion.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/reportes/tenants")]
public sealed class TenantsReportesController(AdminContext db, IConfiguration config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(Guid? despues, CancellationToken ct)
    {
        var clave = config["Reportes:ClaveInterna"] ?? config["Aprovisionamiento:ClaveInterna"];
        if (string.IsNullOrWhiteSpace(clave) ||
            !Request.Headers.TryGetValue("X-Reportes-Interna", out var recibida) || recibida.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString())))
            return NotFound();

        var ahora = DateTimeOffset.UtcNow;
        var activos = db.Tenant.AsNoTracking().Where(t => t.estado == "ACTIVO" && t.Propietario.estado == "ACTIVO" &&
            db.Suscripcion.Any(s => s.tenant_id == t.id && s.usuario_id == t.propietario_id && s.estado == "ACTIVA" &&
                s.desde <= ahora && (s.hasta == null || s.hasta > ahora)));
        if (despues is { } id) activos = activos.Where(t => t.id.CompareTo(id) > 0);
        return Ok(await activos.OrderBy(t => t.id).Select(t => t.id).Take(100).ToArrayAsync(ct));
    }
}
