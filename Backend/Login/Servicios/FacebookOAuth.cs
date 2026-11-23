using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Adminstracion.Dominio;
using Microsoft.AspNetCore.WebUtilities;

namespace Adminstracion.Servicios;

public sealed class FacebookOAuth(IHttpClientFactory clientes, IdentidadesOAuth identidades, IConfiguration config)
{
    public const string Ruta = "/api/Auth/Facebook";
    private const string Version = "v25.0";
    internal const string Emisor = "https://www.facebook.com";
    private const string Graph = "https://graph.facebook.com/" + Version;

    public static bool Habilitado(IConfiguration config) => config.GetValue<bool>("FacebookOAuth:Habilitado");

    public static string Origen(IConfiguration config) => IdentidadesOAuth.Origen(config, "FacebookOAuth");

    public static void VerificarConfiguracion(IConfiguration config)
    {
        if (!Habilitado(config)) return;
        _ = Origen(config);
        var id = config["FacebookOAuth:AppId"];
        if (string.IsNullOrWhiteSpace(id) || !id.All(char.IsAsciiDigit) ||
            string.IsNullOrWhiteSpace(config["FacebookOAuth:AppSecret"]))
            throw new InvalidOperationException("Configura FacebookOAuth:AppId y FacebookOAuth:AppSecret.");
    }

    public string ObtenerUrlAutorizacion(string estado) =>
        QueryHelpers.AddQueryString(Emisor + "/" + Version + "/dialog/oauth",
            new Dictionary<string, string?>
            {
                ["client_id"] = config["FacebookOAuth:AppId"],
                ["redirect_uri"] = Origen(config) + Ruta + "/Callback",
                ["response_type"] = "code",
                ["scope"] = "public_profile,email",
                ["state"] = estado,
            });

    public async Task<Usuario?> AutorizarAsync(string codigo, CancellationToken ct, string? codigoRevisor = null)
    {
        var perfil = await LeerPerfilAsync(codigo, ct);
        return perfil is null ? null : await identidades.ObtenerAsync("FacebookOAuth", Emisor, perfil.Id,
            perfil.Email, perfil.Name, "Usuario de Facebook", ct, codigoRevisor);
    }

    internal async Task<IdentidadFacebook?> LeerPerfilAsync(string codigo, CancellationToken ct)
    {
        using var cliente = clientes.CreateClient("FacebookOAuth");
        var secreto = config["FacebookOAuth:AppSecret"]!;
        var urlToken = QueryHelpers.AddQueryString(Graph + "/oauth/access_token",
            new Dictionary<string, string?>
            {
                ["client_id"] = config["FacebookOAuth:AppId"],
                ["client_secret"] = secreto,
                ["redirect_uri"] = Origen(config) + Ruta + "/Callback",
                ["code"] = codigo,
            });

        using var respuesta = await cliente.GetAsync(urlToken, ct);
        if (!respuesta.IsSuccessStatusCode) return null;
        var autorizacion = await respuesta.Content.ReadFromJsonAsync<Autorizacion>(ct);
        if (string.IsNullOrWhiteSpace(autorizacion?.AccessToken)) return null;

        var prueba = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secreto), Encoding.UTF8.GetBytes(autorizacion.AccessToken));
        var urlPerfil = QueryHelpers.AddQueryString(Graph + "/me", new Dictionary<string, string?>
        {
            ["fields"] = "id,name,email",
            ["appsecret_proof"] = Convert.ToHexStringLower(prueba),
        });
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, urlPerfil);
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", autorizacion.AccessToken);
        using var perfil = await cliente.SendAsync(solicitud, ct);
        if (!perfil.IsSuccessStatusCode) return null;

        var usuario = await perfil.Content.ReadFromJsonAsync<PerfilFacebook>(ct);
        if (usuario?.Id is not { Length: > 0 and <= 64 } sujeto || !sujeto.All(char.IsAsciiDigit))
            return null;

        return new(sujeto, usuario.Name, usuario.Email);
    }

    private sealed record Autorizacion([property: JsonPropertyName("access_token")] string? AccessToken);
    private sealed record PerfilFacebook(string? Id, string? Name, string? Email);
    internal sealed record IdentidadFacebook(string Id, string? Name, string? Email);
}
