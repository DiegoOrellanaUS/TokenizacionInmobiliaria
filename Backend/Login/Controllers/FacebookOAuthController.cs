using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Adminstracion.Controllers;

[ApiController]
[Route(FacebookOAuth.Ruta)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class FacebookOAuthController(IConfiguration config, IDataProtectionProvider proteccion,
    FacebookOAuth facebook, SesionesOAuth sesiones) : ControllerBase
{
    private const string CookieInicio = "__Secure-umbral-facebook";
    private readonly ITimeLimitedDataProtector inicio = proteccion.CreateProtector("Umbral.OAuth.Facebook.v1")
        .ToTimeLimitedDataProtector();

    [HttpGet("Estado")]
    public IActionResult Estado() => Ok(new { Habilitado = FacebookOAuth.Habilitado(config) });

    [HttpGet("Iniciar")]
    [EnableRateLimiting("cuentas")]
    public IActionResult Iniciar(string? volver) => IniciarFlujo(volver, null);

    [HttpPost("RegistrarRevisor")]
    [EnableRateLimiting("cuentas")]
    public IActionResult RegistrarRevisor([FromForm] string? volver, [FromForm] string? codigoRevisor)
    {
        if (!FacebookOAuth.Habilitado(config)) return NotFound();
        if (Request.Headers.Origin.Count != 1 ||
            !string.Equals(Request.Headers.Origin[0], FacebookOAuth.Origen(config), StringComparison.Ordinal))
            return StatusCode(403);

        var codigo = codigoRevisor?.Trim();
        return codigo is { Length: > 0 and <= 128 } ? IniciarFlujo(volver, codigo) : Fallo("invitacion");
    }

    private IActionResult IniciarFlujo(string? volver, string? codigoRevisor)
    {
        if (!FacebookOAuth.Habilitado(config)) return NotFound();

        Response.Headers["Referrer-Policy"] = "no-referrer";
        var estado = SesionesOAuth.Secreto();
        var intento = new Intento(estado, CookieSesionOAuth.Destino(volver), codigoRevisor);
        var duracion = TimeSpan.FromMinutes(10);
        Response.Cookies.Append(CookieInicio, inicio.Protect(JsonSerializer.Serialize(intento), duracion),
            Opciones(duracion));
        return Redirect(facebook.ObtenerUrlAutorizacion(estado));
    }

    [HttpGet("Callback")]
    [EnableRateLimiting("cuentas")]
    public async Task<IActionResult> Callback(string? code, string? state, CancellationToken ct)
    {
        if (!FacebookOAuth.Habilitado(config)) return NotFound();

        Response.Headers["Referrer-Policy"] = "no-referrer";
        var sello = Request.Cookies[CookieInicio];
        Response.Cookies.Delete(CookieInicio, Opciones());
        if (sello is not { Length: > 0 and <= 2048 } || state is not { Length: 43 } ||
            code is not { Length: > 0 and <= 4096 })
            return Fallo("facebook");

        try
        {
            var intento = JsonSerializer.Deserialize<Intento>(inicio.Unprotect(sello));
            if (intento is null || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(intento.Estado), Encoding.UTF8.GetBytes(state)))
                return Fallo("facebook");

            var usuario = await facebook.AutorizarAsync(code, ct, intento.CodigoRevisor);
            if (usuario is null) return Fallo("cuenta");

            var tokens = await sesiones.CrearAsync(usuario.id, ct);
            CookieSesionOAuth.Guardar(Response, tokens);
            return LocalRedirect(QueryHelpers.AddQueryString("/acceso",
                new Dictionary<string, string?> { ["facebook"] = "ok", ["volver"] = intento.Destino }));
        }
        catch (InvitacionRevisionInvalidaException)
        {
            return Fallo("invitacion");
        }
        catch (Exception ex) when (ex is CryptographicException or HttpRequestException or JsonException ||
            (ex is OperationCanceledException && !ct.IsCancellationRequested) ||
            ex is Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation } })
        {
            return Fallo("facebook");
        }
    }

    private IActionResult Fallo(string motivo) => LocalRedirect("/acceso?error=" + motivo);

    private static CookieOptions Opciones(TimeSpan? duracion = null) => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = FacebookOAuth.Ruta, MaxAge = duracion,
    };

    private sealed record Intento(string Estado, string Destino, string? CodigoRevisor = null);
}
