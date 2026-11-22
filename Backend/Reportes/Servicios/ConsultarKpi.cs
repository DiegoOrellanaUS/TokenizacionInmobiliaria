using System.Data;
using System.Globalization;
using System.Text.Json;
using Npgsql;
using Reportes.Data;
using Reportes.Exportacion;

namespace Reportes.Servicios;

public sealed record VentaKpi(string Moneda, decimal Unidades, decimal Importe, decimal? PrecioMedio);
public sealed record MesKpi(string Mes, string Moneda, decimal Compras, decimal Importe);
public sealed record OfertaKpi(Guid Id, string Inmueble, string Codigo, string? Red, string? Emisor, decimal Emitidas,
    decimal Ofrecidas, decimal Vendidas, string Moneda, decimal PrecioPublicado, decimal Importe, decimal? PrecioMedio);
public sealed record FuenteKpi(string Nombre, DateTimeOffset? SincronizadoEn);
public sealed record InformeKpi(DateTimeOffset GeneradoEn, bool Listo, IReadOnlyList<FuenteKpi> Fuentes,
    IReadOnlyList<IndicadorKpi> Indicadores, IReadOnlyList<GraficaKpi> Graficas,
    IReadOnlyList<VentaKpi> Ventas, IReadOnlyList<MesKpi> Meses, IReadOnlyList<OfertaKpi> Ofertas);

public sealed class ConsultarKpi(SiloKpi silos)
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-BO");
    private static readonly (string Codigo, string Etiqueta)[] EstadosInmueble =
        [("BORRADOR", "Borrador"), ("EN_REVISION", "En revisión"), ("CON_OBSERVACIONES", "Con observaciones"), ("APROBADO", "Aprobados")];
    private static readonly (string Codigo, string Etiqueta)[] EstadosCompra =
        [("PENDIENTE", "Pendientes"), ("CONFIRMADA", "Confirmadas"), ("FALLIDA", "Fallidas"), ("CANCELADA", "Canceladas")];

    public async Task<InformeKpi> Obtener(Guid tenant, CancellationToken ct)
    {
        await using var db = silos.Conexion(tenant);
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var totales = new Dictionary<(string Indicador, string Grupo, string Moneda), decimal>();
        await using (var cmd = new NpgsqlCommand("""
            SELECT indicador, grupo, moneda, valor FROM reportes.kpi_total
            WHERE valor <> 0
            """, db, tx))
        await using (var lector = await cmd.ExecuteReaderAsync(ct))
            while (await lector.ReadAsync(ct))
                totales.Add((lector.GetString(0), lector.GetString(1), lector.GetString(2)), lector.GetDecimal(3));

        decimal Total(string indicador, string grupo = "", string moneda = "") =>
            totales.GetValueOrDefault((indicador, grupo, moneda));
        var inmuebles = EstadosInmueble.Select(e => new BarraKpi(e.Etiqueta, Total("inmuebles", e.Codigo))).ToArray();
        var compras = EstadosCompra.Select(e => new BarraKpi(e.Etiqueta, Total("compras", e.Codigo))).ToArray();
        var totalInmuebles = totales.Where(t => t.Key.Indicador == "inmuebles").Sum(t => t.Value);
        var vendidas = Total("vendidas");
        var ofrecidas = Total("ofrecidas");
        var emitidas = Total("emitidas");
        var porcentaje = ofrecidas == 0 ? (decimal?)null : decimal.Round(vendidas / ofrecidas * 100, 1);
        var porMoneda = totales.Keys.Where(k => k.Indicador == "monto").Select(k => k.Moneda).Order().Select(moneda =>
        {
            var unidades = Total("unidades_moneda", moneda: moneda);
            var monto = Total("monto", moneda: moneda);
            return new VentaKpi(moneda, unidades, monto, Promedio(monto, unidades));
        }).ToArray();

        var meses = totales.Keys.Where(k => k.Indicador == "compras_mes")
            .OrderByDescending(k => k.Grupo).ThenBy(k => k.Moneda)
            .Select(k => new MesKpi(k.Grupo, k.Moneda, Total("compras_mes", k.Grupo, k.Moneda),
                Total("importe_mes", k.Grupo, k.Moneda))).ToArray();

        var ofertas = new List<OfertaKpi>();
        await using (var cmd = new NpgsqlCommand("""
            SELECT o.referencia, o.datos::text, e.datos::text
            FROM reportes.kpi_entidad o
            LEFT JOIN reportes.kpi_entidad e ON e.tipo = 'emision'
                AND e.referencia = (o.datos->>'inmuebleId')::uuid AND e.datos->>'estado' = 'CONFIRMADA'
            WHERE o.tipo = 'oferta' AND o.datos->>'eliminado' IS DISTINCT FROM 'true'
            ORDER BY o.datos->>'nombre', o.referencia
            """, db, tx))
        await using (var lector = await cmd.ExecuteReaderAsync(ct))
        {
            while (await lector.ReadAsync(ct))
            {
                var id = lector.GetGuid(0);
                using var oferta = JsonDocument.Parse(lector.GetString(1));
                using var emision = lector.IsDBNull(2) ? null : JsonDocument.Parse(lector.GetString(2));
                var datos = oferta.RootElement;
                var moneda = ProyeccionKpi.Texto(datos, "moneda");
                var grupo = id.ToString("D");
                var unidades = Total("vendidas_oferta", grupo);
                var monto = Total("monto_oferta", grupo, moneda);
                ofertas.Add(new(id, ProyeccionKpi.Texto(datos, "nombre"), ProyeccionKpi.Texto(datos, "codigo"),
                    emision?.RootElement.GetProperty("red").GetString(), emision?.RootElement.GetProperty("emisor").GetString(),
                    emision?.RootElement.GetProperty("unidades").GetDecimal() ?? 0, ProyeccionKpi.Numero(datos, "unidades"),
                    unidades, moneda, ProyeccionKpi.Numero(datos, "precio"), monto, Promedio(monto, unidades)));
            }
        }

        var fuentes = new Dictionary<string, DateTimeOffset>();
        await using (var cmd = new NpgsqlCommand("SELECT fuente, sincronizado_en FROM reportes.kpi_fuente", db, tx))
        await using (var lector = await cmd.ExecuteReaderAsync(ct))
            while (await lector.ReadAsync(ct))
                fuentes.Add(lector.GetString(0), lector.GetFieldValue<DateTimeOffset>(1));
        await tx.CommitAsync(ct);

        var estadosFuentes = SincronizarKpi.Fuentes.Select(f =>
            new FuenteKpi(f, fuentes.TryGetValue(f, out var fecha) ? fecha : null)).ToArray();
        var pendientesRevision = Total("inmuebles", "EN_REVISION");
        var confirmadas = Total("compras", "CONFIRMADA");
        var canceladas = Total("compras", "CANCELADA");
        return new(DateTimeOffset.UtcNow, estadosFuentes.All(f => f.SincronizadoEn != null), estadosFuentes,
            [new("Inmuebles registrados", totalInmuebles, "inmuebles"), new("Tokens vendidos", vendidas, "tokens"),
             new("Oferta colocada", porcentaje, "%"), new("Compras confirmadas", Total("compras", "CONFIRMADA"), "compras")],
            [new("Estado de los inmuebles", "inmuebles",
                $"Inmuebles registrados: {Entero(totalInmuebles)}. Pendientes de una decisión de revisión: {Entero(pendientesRevision)}.", inmuebles),
             new("Emisión y colocación de tokens", "tokens", ofrecidas == 0
                ? "La emisión registra los tokens confirmados en Stellar. El porcentaje de colocación se calcula cuando existe una oferta."
                : $"Se vendieron {Entero(vendidas)} de {Entero(ofrecidas)} tokens ofrecidos. Eso representa el {porcentaje!.Value.ToString("N1", Cultura)} % de colocación.",
                [new("Emitidos", emitidas), new("Ofrecidos", ofrecidas), new("Vendidos", vendidas)]),
             new("Estado de las compras", "compras",
                $"Compras confirmadas: {Entero(confirmadas)}. Canceladas: {Entero(canceladas)}. Los importes y las unidades vendidas se calculan con compras confirmadas.", compras)],
            porMoneda, meses, ofertas);
    }

    private static decimal? Promedio(decimal importe, decimal unidades) =>
        unidades == 0 ? null : decimal.Round(importe / unidades, 2);

    private static string Entero(decimal cantidad) => cantidad.ToString("N0", Cultura);
}
