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
    IWebHostEnvironment environment, IConfiguration configuration, VerificarPagoPollar verificarPago) : ControllerBase
{
    [HttpGet("pollar")]
    public IActionResult ConfiguracionPollar(Guid tenantId)
    {
        if (!Autorizada() || tenantId == Guid.Empty) return NotFound();
        var clavePublica = configuration["Pollar:ClavePublica"];
        var destino = configuration["Pollar:Destino"];
        var precioUnitario = configuration.GetValue("Pollar:PrecioUsdc", 1m);
        if (string.IsNullOrWhiteSpace(clavePublica) || !VerificarPagoPollar.EsCuenta(destino) ||
            precioUnitario is <= 0 or > 10000 || decimal.Round(precioUnitario, 2) != precioUnitario)
            return StatusCode(503, "El pago Pollar en Testnet aún no está configurado.");
        return Ok(new { clavePublica, destino, emisor = VerificarPagoPollar.EmisorUsdc, precioUnitario });
    }

    [HttpPost("{compraId:guid}/pago")]
    public async Task<IActionResult> ConfirmarPago(Guid tenantId, Guid compraId, VerificarPagoPollar.Evidencia pago, CancellationToken ct)
    {
        if (!Autorizada() || tenantId == Guid.Empty || compraId == Guid.Empty)
            return NotFound();
        var resultado = await verificarPago.Ejecutar(compraId, pago, ct);
        return Ok(resultado);
    }

    [HttpPost("{compraId:guid}")]
    public async Task<IActionResult> Liquidar(Guid tenantId, Guid compraId, Solicitud compra, CancellationToken ct)
    {
        if (!Autorizada()) return NotFound();
        if (tenantId == Guid.Empty || compraId == Guid.Empty || compra.InmuebleId == Guid.Empty || compra.CompradorId == Guid.Empty)
            return BadRequest("La solicitud contiene un identificador inválido.");

        if (compra.SinPago && compra.Pago is not null)
            return BadRequest("Una compra demo sin cobro no puede incluir un pago.");
        if (compra.InmuebleId == VerificarPagoPollar.InmuebleCasaPrado && !compra.SinPago && compra.Pago is null)
            return Conflict("Casa Prado requiere un pago USDC confirmado.");

        tenant.TenantId = tenantId;
        var context = HttpContext.RequestServices.GetRequiredService<TokenizacionContext>();
        await silo.EjecutarAsync(tenantId, context, ct);
        var servicio = HttpContext.RequestServices.GetRequiredService<TransferirCompraDemo>();
        try
        {
            if (compra.Pago is not null && (await verificarPago.Ejecutar(compraId, compra.Pago, ct)).Ledger is null)
                return Conflict("El pago USDC aún no está confirmado.");
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

    private bool Autorizada()
    {
        var clave = configuration["Comercio:ClaveLiquidacionDemo"];
        return (environment.IsDevelopment() || environment.IsEnvironment("Piloto")) && !string.IsNullOrWhiteSpace(clave) &&
            Request.Headers.TryGetValue("X-Liquidacion-Demo", out var recibida) && recibida.Count == 1 &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString()));
    }

    public sealed record Solicitud(Guid InmuebleId, Guid CompradorId,
        [Range(typeof(long), "1", "922337203685", ParseLimitsInInvariantCulture = true)] long Unidades,
        VerificarPagoPollar.Evidencia? Pago, bool SinPago = false);
}
