using System.Security.Cryptography;
using System.Text;
using Adminstracion.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/global/tenants")]
public sealed class TenantsGlobalController(AdminContext db, IConfiguration config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(Guid? despues, CancellationToken ct)
    {
        var clave = config["Global:ClaveEventos"];
        if (string.IsNullOrWhiteSpace(clave) ||
            !Request.Headers.TryGetValue("X-Global-Eventos", out var recibida) || recibida.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString())))
            return NotFound();

        var ahora = DateTimeOffset.UtcNow;
        var tenants = db.Tenant.AsNoTracking();
        if (despues is { } id) tenants = tenants.Where(t => t.id.CompareTo(id) > 0);
        return Ok(await tenants.OrderBy(t => t.id).Take(100).Select(t => new
        {
            Id = t.id,
            Publicable = t.estado == "ACTIVO" && t.Propietario.estado == "ACTIVO" &&
                db.Suscripcion.Any(s => s.tenant_id == t.id && s.usuario_id == t.propietario_id && s.estado == "ACTIVA" &&
                    s.desde <= ahora && (s.hasta == null || s.hasta > ahora))
        }).ToArrayAsync(ct));
    }
}
