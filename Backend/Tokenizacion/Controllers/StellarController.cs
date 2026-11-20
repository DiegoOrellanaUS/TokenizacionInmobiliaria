using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Tokenizacion.Multitenancy;
using Tokenizacion.Servicios;

namespace Tokenizacion.Controllers;

[ApiController]
[Route("api/Tokenizacion/Stellar")]
public class StellarController(
    StellarCli stellar,
    TenantContext tenant,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("CrearCuentas", Name = "CrearCuentasStellar")]
    public Task<IActionResult> PostCrearCuentas(CancellationToken cancellationToken) =>
        EjecutarAsync(() => stellar.CrearCuentasAsync(tenant.TenantId, cancellationToken), cancellationToken);

    [HttpPost("CrearLineaConfianza", Name = "CrearLineaConfianzaStellar")]
    public Task<IActionResult> PostCrearLineaConfianza(CancellationToken cancellationToken) =>
        EjecutarAsync(() => stellar.CrearLineaConfianzaAsync(tenant.TenantId, cancellationToken), cancellationToken);

    [HttpPost("Emitir", Name = "EmitirStellar")]
    public Task<IActionResult> PostEmitir(CancellationToken cancellationToken) =>
        EjecutarAsync(() => stellar.EmitirAsync(tenant.TenantId, cancellationToken), cancellationToken);

    [HttpGet("Saldo", Name = "SaldoStellar")]
    public Task<IActionResult> GetSaldo(CancellationToken cancellationToken) =>
        EjecutarAsync(() => stellar.ConsultarSaldoAsync(tenant.TenantId, cancellationToken), cancellationToken);

    private async Task<IActionResult> EjecutarAsync(Func<Task<object>> operacion, CancellationToken cancellationToken)
    {
        var claveConfigurada = configuration["Stellar:ClaveDemo"];
        if (!environment.IsDevelopment() || string.IsNullOrWhiteSpace(claveConfigurada))
            return NotFound();

        if (!Request.Headers.TryGetValue("X-Stellar-Demo-Key", out var clave) || clave.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(clave[0] ?? string.Empty),
                Encoding.UTF8.GetBytes(claveConfigurada)))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await stellar.EjecutarEnOrdenAsync(operacion, cancellationToken));
        }
        catch (StellarRequisitoException ex)
        {
            return Conflict(ex.Message);
        }
        catch (StellarCliException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, ex.Message);
        }
        catch (TimeoutException ex)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, ex.Message);
        }
    }
}
