using System.Net;
using InmueblesOperacion.Data;

namespace InmueblesOperacion.Multitenancy;

public sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, TenantContext tenant, IHttpClientFactory factory,
        IConfiguration configuration, PrepararSilo silo)
    {
        if (!http.Request.Path.StartsWithSegments("/api/Inmuebles"))
        {
            await next(http);
            return;
        }

        var headers = http.Request.Headers;
        if (!headers.TryGetValue("X-Tenant", out var values) || values.Count != 1 ||
            !Guid.TryParse(values[0], out var tenantId) || tenantId == Guid.Empty)
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            await http.Response.WriteAsync("Debes enviar un X-Tenant válido.");
            return;
        }

        if (headers.Authorization.Count != 1)
        {
            RechazarCredenciales(http);
            return;
        }

        var clave = configuration["Inmuebles:ClaveInterna"];
        if (string.IsNullOrWhiteSpace(clave))
        {
            http.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        var ct = http.RequestAborted;
        Acceso? acceso;
        try
        {
            using var client = factory.CreateClient("AdminInmuebles");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"interno/inmuebles/{tenantId:D}/acceso");
            request.Headers.TryAddWithoutValidation("Authorization", headers.Authorization.ToString());
            request.Headers.Add("X-Inmuebles-Interna", clave);
            using var respuesta = await client.SendAsync(request, ct);
            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                RechazarCredenciales(http);
                return;
            }

            if (respuesta.StatusCode == HttpStatusCode.Forbidden)
            {
                http.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            respuesta.EnsureSuccessStatusCode();
            acceso = await respuesta.Content.ReadFromJsonAsync<Acceso>(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException ||
                                  ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            http.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        if (acceso is null || acceso.UsuarioId == Guid.Empty || (!acceso.EsTitular && !acceso.EsRevisor))
        {
            http.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        tenant.TenantId = tenantId;
        tenant.UsuarioId = acceso.UsuarioId;
        tenant.EsTitular = acceso.EsTitular;
        tenant.EsRevisor = acceso.EsRevisor;
        await silo.EjecutarAsync(tenantId, http.RequestServices.GetRequiredService<InmueblesOperacionContext>(), ct);
        await next(http);
    }

    private static void RechazarCredenciales(HttpContext http)
    {
        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
        http.Response.Headers.WWWAuthenticate = "Basic realm=\"Inmuebles\", charset=\"UTF-8\"";
    }

    private sealed record Acceso(Guid UsuarioId, bool EsTitular, bool EsRevisor);
}
