using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Reportes.Data;

namespace Reportes.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/tenants/{tenantId:guid}")]
public sealed class AprovisionamientoController(SiloKpi silo, IConfiguration config, IWebHostEnvironment environment)
    : ControllerBase
{
    [HttpPost("preparar")]
    public async Task<IActionResult> Preparar(Guid tenantId, CancellationToken ct)
    {
        if (!Autorizado(tenantId)) return NotFound();
        await silo.Preparar(tenantId, ct);
        return NoContent();
    }

    [HttpDelete("base")]
    public async Task<IActionResult> Borrar(Guid tenantId, CancellationToken ct)
    {
        if (!environment.IsDevelopment() || !Autorizado(tenantId)) return NotFound();
        await silo.Eliminar(tenantId, ct);
        return NoContent();
    }

    private bool Autorizado(Guid tenantId)
    {
        var clave = config["Aprovisionamiento:ClaveInterna"];
        return tenantId != Guid.Empty && !string.IsNullOrWhiteSpace(clave) &&
            Request.Headers.TryGetValue("X-Aprovisionamiento-Interna", out var recibida) && recibida.Count == 1 &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString()));
    }
}
