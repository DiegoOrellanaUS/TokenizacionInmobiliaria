using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Tokenizacion.Data;
using Tokenizacion.Multitenancy;
using Tokenizacion.Servicios;

namespace Tokenizacion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/rentas-demo/{tenantId:guid}")]
public sealed class RentasDemoInternasController(IWebHostEnvironment environment, IConfiguration config,
    TenantContext tenant, StellarCli stellar, RedStellar red) : ControllerBase
{
    [HttpGet("emision/{inmuebleId:guid}")]
    public async Task<IActionResult> Emision(Guid tenantId, Guid inmuebleId, CancellationToken ct)
    {
        if (!Autorizado()) return NotFound();
        tenant.TenantId = tenantId;
        var services = HttpContext.RequestServices;
        var context = services.GetRequiredService<TokenizacionContext>();
        await services.GetRequiredService<PrepararSiloTokenizacion>().EjecutarAsync(tenantId, context, ct);
        var emision = await services.GetRequiredService<EmitirInmueble>().ConsultarAsync(inmuebleId, ct);
        return emision is { Estado: "CONFIRMADA" } ? Ok(new { emision.Unidades, emision.Codigo }) : NotFound();
    }

    [HttpPost("{cierreId:guid}/firmar")]
    public async Task<IActionResult> Firmar(Guid tenantId, Guid cierreId, Destino pago, CancellationToken ct)
    {
        if (!Autorizado()) return NotFound();
        if (pago.Stroops <= 0 || !Regex.IsMatch(pago.Billetera, @"\AG[A-Z2-7]{55}\z")) return BadRequest();
        try
        {
            var firmado = await stellar.FirmarRentaDemo(tenantId, cierreId, pago.Billetera, pago.Stroops, ct);
            return Ok(new { firmado.Hash, firmado.Sobre });
        }
        catch (Exception e) when (!ct.IsCancellationRequested &&
            e is StellarCliException or HttpRequestException or TimeoutException)
        {
            return StatusCode(503, "Testnet no pudo preparar el pago de renta.");
        }
    }

    [HttpPost("enviar")]
    public async Task<IActionResult> Enviar(Guid tenantId, Envio envio, CancellationToken ct)
    {
        if (!Autorizado()) return NotFound();
        if (!Regex.IsMatch(envio.Hash, @"\A[0-9a-f]{64}\z") || envio.Sobre.Length is < 1 or > 8192) return BadRequest();
        try
        {
            var estado = await red.ConfirmarOEnviarAsync(tenantId, envio.Hash, envio.Sobre, ct);
            return Ok(new { estado.Estado, estado.Ledger });
        }
        catch (Exception e) when (!ct.IsCancellationRequested &&
            e is StellarCliException or HttpRequestException or TimeoutException)
        {
            return StatusCode(503, "Testnet aún no confirmó el pago de renta.");
        }
    }

    private bool Autorizado()
    {
        var clave = config["Comercio:ClaveLiquidacionDemo"];
        return environment.IsDevelopment() && !string.IsNullOrWhiteSpace(clave) &&
            Request.Headers.TryGetValue("X-Liquidacion-Demo", out var recibida) && recibida.Count == 1 &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString()));
    }

    public sealed record Destino(string Billetera, long Stroops);
    public sealed record Envio(string Hash, string Sobre);
}
