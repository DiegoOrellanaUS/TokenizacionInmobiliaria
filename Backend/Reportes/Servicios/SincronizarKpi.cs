using Reportes.Data;

namespace Reportes.Servicios;

public sealed class SincronizarKpi(IHttpClientFactory clientes, IConfiguration config, SiloKpi silos,
    ProyeccionKpi proyeccion, ILogger<SincronizarKpi> logger) : BackgroundService
{
    public static readonly string[] Fuentes = ["inmuebles", "tokenizacion", "comercio"];

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var reloj = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            if (string.IsNullOrWhiteSpace(Clave)) continue;
            try
            {
                Guid? cursor = null;
                while (!ct.IsCancellationRequested)
                {
                    using var respuesta = await Enviar("admin", HttpMethod.Get,
                        cursor is { } id ? $"interno/reportes/tenants?despues={id:D}" : "interno/reportes/tenants", ct);
                    var pagina = await respuesta.Content.ReadFromJsonAsync<Guid[]>(ct)
                        ?? throw new InvalidDataException("Administración no entregó los tenants.");
                    foreach (var tenant in pagina)
                        await ActualizarTenant(tenant, ct);
                    if (pagina.Length < 100) break;
                    cursor = pagina[^1];
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("Se reintentará consultar los tenants de Reportes ({Tipo}).", ex.GetType().Name);
            }
        } while (await reloj.WaitForNextTickAsync(ct));
    }

    private async Task ActualizarTenant(Guid tenant, CancellationToken ct)
    {
        try { await silos.Preparar(tenant, ct); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("No se pudo preparar Reportes para {Tenant} ({Tipo}).", tenant, ex.GetType().Name);
            return;
        }

        foreach (var fuente in Fuentes)
        {
            try
            {
                var ruta = $"interno/tenants/{tenant:D}/eventos-reportes";
                for (var lote = 0; lote < 10; lote++)
                {
                    using var respuesta = await Enviar(fuente, HttpMethod.Get, ruta, ct);
                    var eventos = await respuesta.Content.ReadFromJsonAsync<EventoKpi[]>(ct)
                        ?? throw new InvalidDataException("La fuente no devolvió un lote.");
                    await proyeccion.Aplicar(tenant, fuente, eventos, eventos.Length < 200, ct);
                    if (eventos.Length > 0)
                    {
                        using var confirmacion = await Enviar(fuente, HttpMethod.Post, ruta + "/confirmar", ct,
                            JsonContent.Create(eventos.Select(e => e.Id).ToArray()));
                    }
                    if (eventos.Length < 200) break;
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("Se reintentará sincronizar {Fuente} de {Tenant} ({Tipo}).",
                    fuente, tenant, ex.GetType().Name);
            }
        }
    }

    private string? Clave => config["Reportes:ClaveInterna"] ?? config["Aprovisionamiento:ClaveInterna"];

    private async Task<HttpResponseMessage> Enviar(string fuente, HttpMethod metodo, string ruta, CancellationToken ct,
        HttpContent? contenido = null)
    {
        using var client = clientes.CreateClient(fuente);
        using var request = new HttpRequestMessage(metodo, ruta) { Content = contenido };
        request.Headers.Add("X-Reportes-Interna", Clave);
        var respuesta = await client.SendAsync(request, ct);
        if (respuesta.IsSuccessStatusCode) return respuesta;

        var estado = respuesta.StatusCode;
        respuesta.Dispose();
        throw new HttpRequestException($"La fuente {fuente} respondió {(int)estado}.", null, estado);
    }
}
