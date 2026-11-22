using System.Net;
using Global.Data;

namespace Global.Servicios;

public sealed class SincronizarGlobal(IConfiguration config, IHttpClientFactory clientes, AlmacenGlobal almacen,
    ProyeccionGlobal proyeccion, ILogger<SincronizarGlobal> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var reloj = new PeriodicTimer(TimeSpan.FromSeconds(4));
        do
        {
            if (!almacen.Configurado || string.IsNullOrWhiteSpace(config["Global:ClaveEventos"])) continue;
            try
            {
                await almacen.Preparar(ct);
                await Ciclo(ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("Se reintentará la sincronización global ({Tipo}).", ex.GetType().Name);
            }
        } while (await reloj.WaitForNextTickAsync(ct));
    }

    private async Task Ciclo(CancellationToken ct)
    {
        // Comercio publica un flujo compartido, incluso para ofertas de tenants que dejaron de estar activos.
        await Sincronizar("comercio", "interno/global/eventos-comercio", null, ct);

        var tenants = await ConsultarTenants(ct);
        await proyeccion.ActualizarTenants(tenants, ct);
        foreach (var fuente in new[] { "inmuebles", "tokenizacion" })
        {
            var completo = true;
            foreach (var tenant in tenants)
            {
                var estado = await Sincronizar(fuente, $"interno/tenants/{tenant.Id:D}/eventos-global", tenant.Id, ct);
                completo &= estado == EstadoFuente.AlDia || estado == EstadoFuente.SinSilo && !tenant.Publicable;
            }
            if (completo) await proyeccion.Marcar(fuente, ct);
        }
    }

    private async Task<TenantGlobal[]> ConsultarTenants(CancellationToken ct)
    {
        var tenants = new List<TenantGlobal>();
        var ruta = "interno/global/tenants";
        while (true)
        {
            using var respuesta = await Enviar("admin", HttpMethod.Get, ruta, ct);
            respuesta.EnsureSuccessStatusCode();
            var pagina = await respuesta.Content.ReadFromJsonAsync<TenantGlobal[]>(ct)
                ?? throw new InvalidDataException("Administración no entregó los tenants.");
            tenants.AddRange(pagina);
            if (pagina.Length < 100) return [.. tenants];
            ruta = $"interno/global/tenants?despues={pagina[^1].Id:D}";
        }
    }

    private async Task<EstadoFuente> Sincronizar(string fuente, string ruta, Guid? tenant, CancellationToken ct)
    {
        try
        {
            for (var lote = 0; lote < 10; lote++)
            {
                using var respuesta = await Enviar(fuente, HttpMethod.Get, ruta, ct);
                if (respuesta.StatusCode == HttpStatusCode.NotFound && tenant.HasValue)
                    return EstadoFuente.SinSilo;
                respuesta.EnsureSuccessStatusCode();
                var eventos = await respuesta.Content.ReadFromJsonAsync<EventoGlobal[]>(ct)
                    ?? throw new InvalidDataException("La fuente no entregó los eventos.");

                if (eventos.Length > 0)
                {
                    await proyeccion.Aplicar(fuente, eventos, tenant, ct);
                    using var recibido = await Enviar(fuente, HttpMethod.Post, ruta + "/confirmar", ct,
                        JsonContent.Create(eventos.Select(e => e.Id).ToArray()));
                    recibido.EnsureSuccessStatusCode();
                }

                if (eventos.Length < 200)
                {
                    if (!tenant.HasValue) await proyeccion.Marcar(fuente, ct);
                    return EstadoFuente.AlDia;
                }
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Se reintentará la fuente global {Fuente} para {Tenant} ({Tipo}).",
                fuente, tenant, ex.GetType().Name);
        }

        return EstadoFuente.Pendiente;
    }

    private enum EstadoFuente { Pendiente, AlDia, SinSilo }

    private async Task<HttpResponseMessage> Enviar(string fuente, HttpMethod metodo, string ruta, CancellationToken ct,
        HttpContent? contenido = null)
    {
        using var client = clientes.CreateClient(fuente);
        using var request = new HttpRequestMessage(metodo, ruta) { Content = contenido };
        request.Headers.Add("X-Global-Eventos", config["Global:ClaveEventos"]);
        return await client.SendAsync(request, ct);
    }
}
