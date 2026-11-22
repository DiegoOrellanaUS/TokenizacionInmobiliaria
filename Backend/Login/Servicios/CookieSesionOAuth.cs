namespace Adminstracion.Servicios;

public static class CookieSesionOAuth
{
    public const string Ruta = "/api/Auth/Sesion";
    private const string Nombre = "__Secure-umbral-sesion";
    private const string Anterior = "__Secure-umbral-refresh";

    public static string? Leer(HttpRequest request) => request.Cookies[Nombre] ?? request.Cookies[Anterior];

    public static void Guardar(HttpResponse response, SesionesOAuth.TokensOAuth tokens)
    {
        response.Cookies.Append(Nombre, tokens.Refresh,
            Opciones("/api/Auth", tokens.VenceRefresh - DateTimeOffset.UtcNow));
        response.Cookies.Delete(Anterior, Opciones(GitHubOAuth.Ruta));
    }

    public static void Borrar(HttpResponse response)
    {
        response.Cookies.Delete(Nombre, Opciones("/api/Auth"));
        response.Cookies.Delete(Anterior, Opciones(GitHubOAuth.Ruta));
    }

    public static string Destino(string? volver) =>
        volver is not null && volver.StartsWith("/ofertas/", StringComparison.Ordinal) &&
            Guid.TryParseExact(volver.AsSpan(9), "D", out var oferta) ? "/ofertas/" + oferta.ToString("D") : "/espacio";

    private static CookieOptions Opciones(string ruta, TimeSpan? duracion = null) => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = ruta, MaxAge = duracion,
    };
}
