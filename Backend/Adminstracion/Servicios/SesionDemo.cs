using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Adminstracion.Servicios;

public sealed class SesionDemo(IDataProtectionProvider provider, IWebHostEnvironment environment, IConfiguration configuration)
{
    private readonly ITimeLimitedDataProtector protector = provider.CreateProtector("TokenizacionInmobiliaria.SesionDemo.v1")
        .ToTimeLimitedDataProtector();

    public bool Disponible(HttpContext http) =>
        environment.IsEnvironment("Piloto") ||
        (environment.IsDevelopment() && configuration.GetValue<bool>("SesionDemo:Habilitada") &&
         http.Connection.RemoteIpAddress is { } remote && IPAddress.IsLoopback(remote));

    public string Emitir(Guid usuarioId) => protector.Protect(usuarioId.ToString("D"), TimeSpan.FromHours(2));

    public bool Leer(HttpContext http, out Guid usuarioId)
    {
        usuarioId = Guid.Empty;
        if (!Disponible(http) || http.Request.Headers.Authorization.Count != 1)
            return false;

        var header = http.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || header.Length > 2048)
            return false;

        try
        {
            return Guid.TryParse(protector.Unprotect(header[7..]), out usuarioId) && usuarioId != Guid.Empty;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
