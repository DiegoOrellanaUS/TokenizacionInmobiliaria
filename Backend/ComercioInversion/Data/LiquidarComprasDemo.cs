using Microsoft.EntityFrameworkCore;

namespace ComercioInversion.Data;

public sealed class LiquidarComprasDemo(IServiceScopeFactory scopes, IHttpClientFactory clientes,
    IWebHostEnvironment environment, IConfiguration configuration, ILogger<LiquidarComprasDemo> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if ((!environment.IsDevelopment() && !environment.IsEnvironment("Piloto")) ||
            string.IsNullOrWhiteSpace(configuration["Comercio:ClaveLiquidacionDemo"]))
            return;

        using var reloj = new PeriodicTimer(TimeSpan.FromSeconds(4));
        while (await reloj.WaitForNextTickAsync(stoppingToken))
        {
            try { await ProcesarAsync(stoppingToken); }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("La liquidación de demostración se reintentará ({Tipo}).", ex.GetType().Name);
            }
        }
    }

    private async Task ProcesarAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogoContext>();
        await scope.ServiceProvider.GetRequiredService<PrepararCatalogo>().Ejecutar(context, ct);
        var ahora = DateTimeOffset.UtcNow;
        var candidatas = await context.Compras.AsNoTracking().Where(c => c.Estado == "PENDIENTE" && c.ReintentarEn <= ahora &&
                (c.OfertaId != CompraDemo.OfertaCasaPrado || c.HashPago != null && c.LedgerPago != null))
            .OrderBy(c => c.ReintentarEn).Take(10).Include(c => c.Oferta).ToListAsync(ct);
        foreach (var compra in candidatas)
        {
            var inicio = DateTimeOffset.UtcNow;
            var adquirida = await context.Compras.Where(c => c.Id == compra.Id && c.Estado == "PENDIENTE" && c.ReintentarEn <= inicio)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ReintentarEn, inicio.AddMinutes(2))
                    .SetProperty(c => c.TransferenciaIniciada, true), ct);
            if (adquirida == 0)
                continue;

            try { await LiquidarAsync(context, compra, ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                await Reintentar(context, compra.Id, ct);
                logger.LogWarning("La compra {CompraId} sigue pendiente ({Tipo}).", compra.Id, ex.GetType().Name);
            }
        }
    }

    private async Task LiquidarAsync(CatalogoContext context, CompraDemo compra, CancellationToken ct)
    {
        using var client = clientes.CreateClient("LiquidacionDemo");
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"interno/tenants/{compra.Oferta.TenantId:D}/compras-demo/{compra.Id:D}");
        request.Headers.Add("X-Liquidacion-Demo", configuration["Comercio:ClaveLiquidacionDemo"]);
        request.Content = JsonContent.Create(new
        {
            compra.Oferta.InmuebleId, compra.CompradorId, compra.Unidades,
            Pago = compra.HashPago is null ? null : new CompraDemo.EvidenciaPago(compra.HashPago,
                compra.PagadorUsdc!, compra.DestinoUsdc!, compra.PrecioUnitario * compra.Unidades, compra.CreadaEn),
        });
        using var response = await client.SendAsync(request, ct);
        var resultado = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<Resultado>(ct)
            : null;

        if (resultado?.Estado is not ("CONFIRMADA" or "FALLIDA") ||
            resultado.Estado == "CONFIRMADA" &&
                (resultado.Ledger is null or <= 0 || string.IsNullOrWhiteSpace(resultado.Hash) ||
                 string.IsNullOrWhiteSpace(resultado.Billetera)))
        {
            await Reintentar(context, compra.Id, ct);
            return;
        }

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        var actualizado = await context.Compras.Where(c => c.Id == compra.Id && c.Estado == "PENDIENTE")
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, resultado.Estado)
                .SetProperty(c => c.Hash, resultado.Hash).SetProperty(c => c.Billetera, resultado.Billetera)
                .SetProperty(c => c.Ledger, resultado.Ledger), ct);
        if (actualizado == 1 && resultado.Estado == "FALLIDA")
            await context.Ofertas.Where(o => o.Id == compra.OfertaId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Disponibles, o => o.Disponibles + compra.Unidades), ct);
        await tx.CommitAsync(ct);
    }

    private static Task Reintentar(CatalogoContext context, Guid id, CancellationToken ct) =>
        context.Compras.Where(c => c.Id == id && c.Estado == "PENDIENTE")
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ReintentarEn, DateTimeOffset.UtcNow.AddSeconds(8)), ct);

    private sealed record Resultado(string Estado, string? Hash, string? Billetera, long? Ledger);
}
