using System.Security.Cryptography;
using System.Text;
using Tokenizacion.Data;
using Tokenizacion.Multitenancy;
using Tokenizacion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Tokenizacion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/tenants/{tenantId:guid}")]
public sealed class AprovisionamientoController(TenantContext tenant, PrepararSiloTokenizacion silo, IConfiguration configuration, IWebHostEnvironment environment)
    : ControllerBase
{
    [HttpPost("preparar")]
    public async Task<IActionResult> Preparar(Guid tenantId, CancellationToken ct)
    {
        if (!Autorizado())
            return NotFound();
        if (tenantId == Guid.Empty)
            return BadRequest("El tenant no es válido.");

        tenant.TenantId = tenantId;
        var context = HttpContext.RequestServices.GetRequiredService<TokenizacionContext>();
        await silo.EjecutarAsync(tenantId, context, ct);
        return NoContent();
    }

    [HttpDelete("base")]
    public async Task<IActionResult> Eliminar(Guid tenantId, CancellationToken ct)
    {
        if (!environment.IsDevelopment() || !Autorizado())
            return NotFound();
        if (tenantId == Guid.Empty)
            return BadRequest("El tenant no es válido.");

        tenant.TenantId = tenantId;
        var context = HttpContext.RequestServices.GetRequiredService<TokenizacionContext>();
        await silo.EliminarAsync(tenantId, context, ct);
        return NoContent();
    }

    private bool Autorizado()
    {
        var clave = configuration["Aprovisionamiento:ClaveInterna"];
        return !string.IsNullOrWhiteSpace(clave) &&
            Request.Headers.TryGetValue("X-Aprovisionamiento-Interna", out var recibida) && recibida.Count == 1 &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString()));
    }
}
