using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Tokenizacion.Data;
using Tokenizacion.Multitenancy;
using Tokenizacion.Servicios;

namespace Tokenizacion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/tenants/{tenantId:guid}/compras-demo")]
public sealed class ComprasDemoInternasController(TenantContext tenant, PrepararSiloTokenizacion silo,
    IWebHostEnvironment environment, IConfiguration configuration) : ControllerBase
{
    [HttpPost("{compraId:guid}")]
    public async Task<IActionResult> Liquidar(Guid tenantId, Guid compraId, Solicitud compra, CancellationToken ct)
    {
        var clave = configuration["Comercio:ClaveLiquidacionDemo"];
        if ((!environment.IsDevelopment() && !environment.IsEnvironment("Piloto")) || string.IsNullOrWhiteSpace(clave) ||
            !Request.Headers.TryGetValue("X-Liquidacion-Demo", out var recibida) || recibida.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString())))
            return NotFound();
        if (tenantId == Guid.Empty || compraId == Guid.Empty || compra.InmuebleId == Guid.Empty || compra.CompradorId == Guid.Empty)
            return BadRequest("La solicitud contiene un identificador inválido.");

        tenant.TenantId = tenantId;
        var context = HttpContext.RequestServices.GetRequiredService<TokenizacionContext>();
        await silo.EjecutarAsync(tenantId, context, ct);
        var servicio = HttpContext.RequestServices.GetRequiredService<TransferirCompraDemo>();
        try
        {
            var t = await servicio.Ejecutar(tenantId, compraId, compra.InmuebleId, compra.CompradorId, compra.Unidades, ct);
            return Ok(new { t.Estado, t.Hash, t.Billetera, t.Ledger });
        }
        catch (StellarRequisitoException)
        {
            return Conflict("No pudo conciliarse la orden interna.");
        }
        catch (Exception ex) when (ex is HttpRequestException or StellarCliException or TimeoutException ||
            ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            return StatusCode(503, "Stellar aún no pudo confirmar la transferencia.");
        }
    }

    public sealed record Solicitud(Guid InmuebleId, Guid CompradorId,
        [Range(typeof(long), "1", "922337203685", ParseLimitsInInvariantCulture = true)] long Unidades);
}
