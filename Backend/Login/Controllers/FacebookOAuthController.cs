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
    FacebookOAuth facebook, SesionesOAuth sesiones, IdentidadesOAuth identidades) : ControllerBase
{
    private const string CookieInicio = "__Secure-umbral-facebook";
    private const string CookieVinculo = "__Secure-umbral-facebook-vinculo";
    private const string Conexiones = "/cuenta/conexiones";
    private readonly ITimeLimitedDataProtector inicio = proteccion.CreateProtector("Umbral.OAuth.Facebook.v1")
        .ToTimeLimitedDataProtector();
    private readonly ITimeLimitedDataProtector vinculo = proteccion.CreateProtector("Umbral.OAuth.Facebook.Vinculo.v1")
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
        if (!OrigenPermitido()) return StatusCode(403);

        var codigo = codigoRevisor?.Trim();
        return codigo is { Length: > 0 and <= 128 } ? IniciarFlujo(volver, codigo) : Fallo("invitacion");
    }

    [HttpGet("Vinculo")]
    public async Task<IActionResult> EstadoVinculo(CancellationToken ct)
    {
        if (!FacebookOAuth.Habilitado(config)) return NotFound();
        var sesion = await sesiones.ConsultarActivaAsync(CookieSesionOAuth.Leer(Request), ct);
        if (sesion is null) return Unauthorized();
        var esGitHub = sesion.EmisorPrincipal == GitHubOAuth.Emisor;
        var vinculado = sesion.EmisorPrincipal == FacebookOAuth.Emisor ||
            (esGitHub && await identidades.TieneFacebookAsync(sesion.UsuarioId, ct));
        var pendiente = esGitHub && !vinculado ? LeerVinculo(sesion) : null;
        return Ok(new { Vinculado = vinculado, PuedeVincular = esGitHub && !vinculado, FacebookPendiente = pendiente?.Nombre });
    }

    [HttpPost("Vincular")]
    [EnableRateLimiting("cuentas")]
    public async Task<IActionResult> Vincular(CancellationToken ct)
    {
        if (!FacebookOAuth.Habilitado(config)) return NotFound();
        if (!OrigenPermitido()) return StatusCode(403);
        var sesion = await sesiones.ConsultarActivaAsync(CookieSesionOAuth.Leer(Request), ct);
        if (sesion is null) return Unauthorized();
        if (sesion.EmisorPrincipal != GitHubOAuth.Emisor) return StatusCode(403);
        if (await identidades.TieneFacebookAsync(sesion.UsuarioId, ct)) return LocalRedirect(Conexiones);
        Response.Cookies.Delete(CookieVinculo, OpcionesVinculo());
        return IniciarFlujo(null, null, new(sesion.UsuarioId, sesion.FamiliaId));
    }

    [HttpPost("ConfirmarVinculo")]
    [EnableRateLimiting("cuentas")]
    public async Task<IActionResult> ConfirmarVinculo(CancellationToken ct)
    {
        if (!FacebookOAuth.Habilitado(config)) return NotFound();
        if (!OrigenPermitido()) return StatusCode(403);
        var sesion = await sesiones.ConsultarActivaAsync(CookieSesionOAuth.Leer(Request), ct);
        if (sesion is null) return Unauthorized();
        if (sesion.EmisorPrincipal != GitHubOAuth.Emisor) return StatusCode(403);
        var pendiente = LeerVinculo(sesion);
        Response.Cookies.Delete(CookieVinculo, OpcionesVinculo());
        if (pendiente is null) return Conflict("La confirmación venció o cambió tu sesión de Umbral. Inicia la vinculación de nuevo.");
        return await identidades.VincularFacebookAsync(sesion.UsuarioId, pendiente.Sujeto, ct)
            ? NoContent() : Conflict("Esa cuenta de Facebook ya está vinculada o tu perfil ya tiene otra cuenta de Facebook.");
    }

    private IActionResult IniciarFlujo(string? volver, string? codigoRevisor, CuentaVinculo? cuenta = null)
    {
        if (!FacebookOAuth.Habilitado(config)) return NotFound();

        Response.Headers["Referrer-Policy"] = "no-referrer";
        var estado = SesionesOAuth.Secreto();
        var intento = new Intento(estado, CookieSesionOAuth.Destino(volver), codigoRevisor, cuenta);
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
        if (sello is not { Length: > 0 and <= 2048 } || state is not { Length: 43 })
            return Fallo("facebook");

        Intento? intento = null;
        try
        {
            intento = JsonSerializer.Deserialize<Intento>(inicio.Unprotect(sello));
            if (intento is null || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(intento.Estado), Encoding.UTF8.GetBytes(state)))
                return Fallo("facebook");

            if (code is not { Length: > 0 and <= 4096 })
                return intento.Cuenta is null ? Fallo("facebook") : FalloVinculo();
            if (intento.Cuenta is { } cuenta)
            {
                var perfil = await facebook.LeerPerfilAsync(code, ct);
                if (perfil is null) return FalloVinculo();
                var nombre = string.IsNullOrWhiteSpace(perfil.Name) ? "Cuenta de Facebook" : perfil.Name.Trim();
                var datos = new VinculoPendiente(cuenta, perfil.Id, nombre[..Math.Min(nombre.Length, 160)]);
                var duracion = TimeSpan.FromMinutes(5);
                Response.Cookies.Append(CookieVinculo, vinculo.Protect(JsonSerializer.Serialize(datos), duracion),
                    OpcionesVinculo(duracion));
                return LocalRedirect(Conexiones);
            }

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
            return intento?.Cuenta is null ? Fallo("facebook") : FalloVinculo();
        }
    }

    private IActionResult Fallo(string motivo) => LocalRedirect("/acceso?error=" + motivo);
    private IActionResult FalloVinculo() => LocalRedirect(Conexiones + "?facebook=fallo");

    private bool OrigenPermitido() => Request.Headers.Origin.Count == 1 &&
        string.Equals(Request.Headers.Origin[0], FacebookOAuth.Origen(config), StringComparison.Ordinal);

    private VinculoPendiente? LeerVinculo(SesionesOAuth.SesionActiva sesion)
    {
        if (Request.Cookies[CookieVinculo] is not { Length: > 0 and <= 4096 } sello) return null;
        try
        {
            var pendiente = JsonSerializer.Deserialize<VinculoPendiente>(vinculo.Unprotect(sello));
            return pendiente?.Cuenta == new CuentaVinculo(sesion.UsuarioId, sesion.FamiliaId) ? pendiente : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException) { return null; }
    }

    private static CookieOptions Opciones(TimeSpan? duracion = null) => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = FacebookOAuth.Ruta, MaxAge = duracion,
    };

    private static CookieOptions OpcionesVinculo(TimeSpan? duracion = null) => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = FacebookOAuth.Ruta, MaxAge = duracion,
    };

    private sealed record CuentaVinculo(Guid UsuarioId, Guid FamiliaId);
    private sealed record VinculoPendiente(CuentaVinculo Cuenta, string Sujeto, string Nombre);
    private sealed record Intento(string Estado, string Destino, string? CodigoRevisor = null, CuentaVinculo? Cuenta = null);
}
