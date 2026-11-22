using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Adminstracion.Servicios;

public sealed class SesionDemo(IDataProtectionProvider provider, IWebHostEnvironment environment,
    IConfiguration configuration, SesionesOAuth oauth)
{
    private readonly ITimeLimitedDataProtector protector = provider.CreateProtector("TokenizacionInmobiliaria.SesionDemo.v1")
        .ToTimeLimitedDataProtector();

    public bool GitHubHabilitado => configuration.GetValue<bool>("GitHubOAuth:Habilitado");
    public bool FacebookHabilitado => configuration.GetValue<bool>("FacebookOAuth:Habilitado");
    private bool OAuthHabilitado => GitHubHabilitado || FacebookHabilitado;
    public bool SoloOAuth => OAuthHabilitado && !environment.IsEnvironment("Piloto");

    public bool Disponible(HttpContext http) =>
        OAuthHabilitado || environment.IsEnvironment("Piloto") ||
        (environment.IsDevelopment() && configuration.GetValue<bool>("SesionDemo:Habilitada") &&
         http.Connection.RemoteIpAddress is { } remote && IPAddress.IsLoopback(remote));

    private const string IdentidadVerificada = ":pwd";

    public string Emitir(Guid usuarioId, bool identidadVerificada) =>
        protector.Protect(usuarioId.ToString("D") + (identidadVerificada ? IdentidadVerificada : ""), TimeSpan.FromHours(2));

    public async Task<Guid?> LeerAsync(HttpContext http, CancellationToken ct, bool exigirIdentidad = false)
    {
        if (!Disponible(http) || http.Request.Headers.Authorization.Count != 1)
            return null;

        var header = http.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || header.Length > 2048)
            return null;

        var token = header[7..];
        if (OAuthHabilitado && token.Length == 43) return await oauth.ValidarAsync(token, ct);
        if (SoloOAuth) return null;
        try
        {
            var contenido = protector.Unprotect(token);
            var verificada = contenido.EndsWith(IdentidadVerificada, StringComparison.Ordinal);
            if (exigirIdentidad && !verificada) return null;
            var identidad = verificada ? contenido[..^IdentidadVerificada.Length] : contenido;
            return Guid.TryParseExact(identidad, "D", out var usuarioId) && usuarioId != Guid.Empty
                ? usuarioId : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
