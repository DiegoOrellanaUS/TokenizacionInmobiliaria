using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Adminstracion.Data;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Adminstracion.Controllers;

[ApiController]
[Route(GitHubOAuth.Ruta)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GitHubOAuthController(IConfiguration config, IDataProtectionProvider proteccion,
    GitHubOAuth github, SesionesOAuth sesiones, AdminContext context, PerfilSesion perfiles) : ControllerBase
{
    private const string CookieInicio = "__Secure-umbral-github";
    private readonly ITimeLimitedDataProtector inicio = proteccion.CreateProtector("Umbral.OAuth.GitHub.v1")
        .ToTimeLimitedDataProtector();

    [HttpGet("Estado")]
    public IActionResult Estado() => Ok(new { Habilitado = GitHubOAuth.Habilitado(config) });

    [HttpGet("Iniciar")]
    [EnableRateLimiting("cuentas")]
    public IActionResult Iniciar(string? volver) => IniciarFlujo(volver, null);

    [HttpPost("RegistrarRevisor")]
    [EnableRateLimiting("cuentas")]
    public IActionResult RegistrarRevisor([FromForm] string? volver, [FromForm] string? codigoRevisor)
    {
        if (!GitHubOAuth.Habilitado(config)) return NotFound();
        if (Request.Headers.Origin.Count != 1 ||
            !string.Equals(Request.Headers.Origin[0], GitHubOAuth.Origen(config), StringComparison.Ordinal))
            return StatusCode(403);

        var codigo = codigoRevisor?.Trim();
        return codigo is { Length: > 0 and <= 128 } ? IniciarFlujo(volver, codigo) : Fallo("invitacion");
    }

    private IActionResult IniciarFlujo(string? volver, string? codigoRevisor)
    {
        if (!GitHubOAuth.Habilitado(config)) return NotFound();
        Response.Headers["Referrer-Policy"] = "no-referrer";
        var estado = SesionesOAuth.Secreto();
        var verificador = SesionesOAuth.Secreto();
        var datos = new Intento(estado, verificador, CookieSesionOAuth.Destino(volver), codigoRevisor);
        var cookie = inicio.Protect(JsonSerializer.Serialize(datos), TimeSpan.FromMinutes(10));
        Response.Cookies.Append(CookieInicio, cookie, Opciones(SameSiteMode.Lax, TimeSpan.FromMinutes(10)));
        return Redirect(QueryHelpers.AddQueryString("https://github.com/login/oauth/authorize",
            new Dictionary<string, string?>
            {
                ["client_id"] = config["GitHubOAuth:ClientId"],
                ["redirect_uri"] = GitHubOAuth.Origen(config) + GitHubOAuth.Ruta + "/Callback",
                ["scope"] = "read:user user:email",
                ["state"] = estado,
                ["code_challenge"] = SesionesOAuth.Desafio(verificador),
                ["code_challenge_method"] = "S256",
            }));
    }

    [HttpGet("Callback")]
    [EnableRateLimiting("cuentas")]
    public async Task<IActionResult> Callback(string? code, string? state, CancellationToken ct)
    {
        if (!GitHubOAuth.Habilitado(config)) return NotFound();
        Response.Headers["Referrer-Policy"] = "no-referrer";
        var sello = Request.Cookies[CookieInicio];
        Response.Cookies.Delete(CookieInicio, Opciones(SameSiteMode.Lax));
        if (sello is not { Length: > 0 and <= 2048 } || state is not { Length: 43 } ||
            code is not { Length: > 0 and <= 256 })
            return Fallo("github");

        try
        {
            var intento = JsonSerializer.Deserialize<Intento>(inicio.Unprotect(sello));
            if (intento is null || !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(intento.Estado), Encoding.UTF8.GetBytes(state)))
                return Fallo("github");

            var usuario = await github.AutorizarAsync(code, intento.Verificador, ct, intento.CodigoRevisor);
            if (usuario is null) return Fallo("cuenta");

            var tokens = await sesiones.CrearAsync(usuario.id, ct);
            CookieSesionOAuth.Guardar(Response, tokens);
            return LocalRedirect(QueryHelpers.AddQueryString("/acceso",
                new Dictionary<string, string?> { ["github"] = "ok", ["volver"] = intento.Destino }));
        }
        catch (InvitacionRevisionInvalidaException)
        {
            return Fallo("invitacion");
        }
        catch (Exception ex) when (ex is CryptographicException or HttpRequestException or JsonException ||
            (ex is OperationCanceledException && !ct.IsCancellationRequested) ||
            ex is Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation } })
        {
            return Fallo("github");
        }
    }

    [HttpPost("Refrescar")]
    [HttpPost(CookieSesionOAuth.Ruta + "/Refrescar")]
    public async Task<IActionResult> Refrescar(CancellationToken ct)
    {
        if (!FacebookOAuth.Habilitado(config) && !GitHubOAuth.Habilitado(config)) return NotFound();
        if (!OrigenPermitido()) return StatusCode(403);
        var tokens = await sesiones.RenovarAsync(CookieSesionOAuth.Leer(Request), ct);
        if (tokens is null)
        {
            CookieSesionOAuth.Borrar(Response);
            return Unauthorized();
        }

        var usuario = await context.Usuario.FindAsync([tokens.UsuarioId], ct);
        if (usuario is null) return Unauthorized();
        var perfil = await perfiles.ObtenerAsync(usuario, tokens.Access, tokens.VenceAccess, ct);
        CookieSesionOAuth.Guardar(Response, tokens);
        return Ok(perfil);
    }

    [HttpPost("Salir")]
    [HttpPost(CookieSesionOAuth.Ruta + "/Salir")]
    public async Task<IActionResult> Salir(CancellationToken ct)
    {
        if (!FacebookOAuth.Habilitado(config) && !GitHubOAuth.Habilitado(config)) return NotFound();
        if (!OrigenPermitido()) return StatusCode(403);
        await sesiones.RevocarAsync(CookieSesionOAuth.Leer(Request), ct);
        CookieSesionOAuth.Borrar(Response);
        return NoContent();
    }

    private IActionResult Fallo(string motivo) => LocalRedirect("/acceso?error=" + motivo);

    private bool OrigenPermitido() => Request.Headers.Origin.Count == 1 &&
        string.Equals(Request.Headers.Origin[0], FacebookOAuth.Habilitado(config)
            ? FacebookOAuth.Origen(config) : GitHubOAuth.Origen(config), StringComparison.Ordinal);

    private static CookieOptions Opciones(SameSiteMode modo, TimeSpan? duracion = null) => new()
    {
        HttpOnly = true, Secure = true, SameSite = modo, Path = GitHubOAuth.Ruta, MaxAge = duracion,
    };

    private sealed record Intento(string Estado, string Verificador, string Destino, string? CodigoRevisor = null);
}
