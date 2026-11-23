using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Adminstracion.Dominio;

namespace Adminstracion.Servicios;

public sealed class GitHubOAuth(IHttpClientFactory clients, IdentidadesOAuth identidades, IConfiguration config)
{
    public const string Ruta = "/api/Auth/GitHub";
    internal const string Emisor = "https://github.com";

    public static bool Habilitado(IConfiguration config) => config.GetValue<bool>("GitHubOAuth:Habilitado");

    public static string Origen(IConfiguration config) => IdentidadesOAuth.Origen(config, "GitHubOAuth");

    public static void VerificarConfiguracion(IConfiguration config)
    {
        if (!Habilitado(config)) return;
        _ = Origen(config);
        if (string.IsNullOrWhiteSpace(config["GitHubOAuth:ClientId"]) ||
            string.IsNullOrWhiteSpace(config["GitHubOAuth:ClientSecret"]))
            throw new InvalidOperationException("Configura GitHubOAuth:ClientId y GitHubOAuth:ClientSecret.");
    }

    public async Task<Usuario?> AutorizarAsync(string codigo, string verificador, CancellationToken ct, string? codigoRevisor = null)
    {
        using var client = clients.CreateClient("GitHubOAuth");
        using var datos = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = config["GitHubOAuth:ClientId"]!,
            ["client_secret"] = config["GitHubOAuth:ClientSecret"]!,
            ["redirect_uri"] = Origen(config) + Ruta + "/Callback",
            ["code"] = codigo,
            ["code_verifier"] = verificador,
        });
        using var respuesta = await client.PostAsync(Emisor + "/login/oauth/access_token", datos, ct);
        if (!respuesta.IsSuccessStatusCode) return null;
        var autorizacion = await respuesta.Content.ReadFromJsonAsync<Autorizacion>(ct);
        if (string.IsNullOrWhiteSpace(autorizacion?.AccessToken)) return null;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", autorizacion.AccessToken);

        var usuario = await client.GetFromJsonAsync<UsuarioGitHub>("https://api.github.com/user", ct);
        var correos = await client.GetFromJsonAsync<CorreoGitHub[]>("https://api.github.com/user/emails", ct);
        var correo = correos?.FirstOrDefault(c => c.Primary && c.Verified)?.Email.Trim().ToLowerInvariant();
        if (usuario is null || usuario.Id <= 0 || correo is not { Length: > 0 and <= 254 }) return null;

        var sujeto = usuario.Id.ToString(CultureInfo.InvariantCulture);
        var nombre = string.IsNullOrWhiteSpace(usuario.Name) ? usuario.Login : usuario.Name;
        return await identidades.ObtenerAsync("GitHubOAuth", Emisor, sujeto, correo,
            nombre, "Usuario de GitHub", ct, codigoRevisor);
    }

    private sealed record Autorizacion([property: JsonPropertyName("access_token")] string? AccessToken);
    private sealed record UsuarioGitHub(long Id, string? Login, string? Name);
    private sealed record CorreoGitHub(string Email, bool Primary, bool Verified);
}
