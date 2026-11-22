using System.Data;
using Global.Data;
using Npgsql;
using static Global.Data.AlmacenGlobal;

namespace Global.Servicios;

public sealed class ConsultarGlobal(AlmacenGlobal almacen)
{
    public async Task<IReadOnlyList<OfertaGlobal>?> Catalogo(CancellationToken ct, Guid? ofertaId = null)
    {
        await using var db = almacen.Conexion();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var fuentes = await Fechas(db, tx, ct);
        if (!fuentes.ContainsKey("admin") || !fuentes.ContainsKey("comercio")) return null;

        var ofertas = new List<OfertaGlobal>();
        await using (var cmd = Comando(db, tx, """
            SELECT o.id,o.inmueble_id,o.nombre,o.ciudad,o.tipo,o.codigo,o.unidades,o.disponibles,o.precio,o.moneda,o.publicada_en,
                   e.datos->>'emisor', e.datos->>'red'
            FROM global.oferta_publica o JOIN global.tenant_publicacion t ON t.tenant_id=o.tenant_id
            LEFT JOIN global.kpi_entidad e ON e.tenant_id=o.tenant_id AND e.tipo='emision' AND e.referencia=o.inmueble_id
                AND e.datos->>'estado'='CONFIRMADA' AND e.datos->>'codigo'=o.codigo
            WHERE t.publicable AND ($1 = '00000000-0000-0000-0000-000000000000' OR o.id = $1)
            ORDER BY o.publicada_en DESC,o.id LIMIT 100
            """, ofertaId ?? Guid.Empty))
        await using (var lector = await cmd.ExecuteReaderAsync(ct))
            while (await lector.ReadAsync(ct))
                ofertas.Add(new(lector.GetGuid(0), lector.GetGuid(1), lector.GetString(2), lector.GetString(3),
                    lector.GetString(4), lector.GetString(5), lector.GetInt64(6), lector.GetInt64(7),
                    lector.GetDecimal(8), lector.GetString(9), lector.GetFieldValue<DateTimeOffset>(10),
                    lector.IsDBNull(11) ? null : lector.GetString(11), lector.IsDBNull(12) ? null : lector.GetString(12)));

        await tx.CommitAsync(ct);
        return ofertas;
    }

    public async Task<InformeGlobal> Indicadores(CancellationToken ct)
    {
        await using var db = almacen.Conexion();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var filas = new Dictionary<(string Indicador, string Grupo, string Moneda), decimal>();
        await using (var cmd = Comando(db, tx, """
            SELECT indicador,grupo,moneda,valor FROM global.kpi_total WHERE valor<>0
            """))
        await using (var lector = await cmd.ExecuteReaderAsync(ct))
            while (await lector.ReadAsync(ct))
                filas.Add((lector.GetString(0), lector.GetString(1), lector.GetString(2)), lector.GetDecimal(3));
        var fuentes = await Fechas(db, tx, ct);
        await tx.CommitAsync(ct);

        decimal Total(string indicador, string grupo = "", string moneda = "") => filas.GetValueOrDefault((indicador, grupo, moneda));
        var ofrecidas = Total("tokens_ofrecidos");
        var vendidas = Total("tokens_vendidos");
        var sincronizacion = new[] { "admin", "inmuebles", "tokenizacion", "comercio" }.Select(fuente =>
            new FuenteGlobal(fuente, fuentes.TryGetValue(fuente, out var fecha) ? fecha : null)).ToArray();

        return new(DateTimeOffset.UtcNow, sincronizacion.All(f => f.ActualizadoEn.HasValue), sincronizacion,
            Total("tenants", "total"), Total("tenants", "activos"),
            filas.Where(r => r.Key.Indicador == "inmuebles").Sum(r => r.Value),
            Total("inmuebles", "BORRADOR"), Total("inmuebles", "EN_REVISION"),
            Total("inmuebles", "CON_OBSERVACIONES"), Total("inmuebles", "APROBADO"), Total("ofertas"),
            Total("tokens_emitidos"), ofrecidas, vendidas, ofrecidas == 0 ? null : decimal.Round(100 * vendidas / ofrecidas, 1),
            Total("compras", "PENDIENTE"), Total("compras", "CONFIRMADA"), Total("compras", "FALLIDA"), Total("compras", "CANCELADA"),
            filas.Where(r => r.Key.Indicador == "importe_confirmado").OrderBy(r => r.Key.Moneda)
                .Select(r => new ImporteGlobal(r.Key.Moneda, r.Value)).ToArray(),
            filas.Where(r => r.Key.Indicador == "compras_mes").OrderByDescending(r => r.Key.Grupo).ThenBy(r => r.Key.Moneda)
                .Select(r => new MesGlobal(r.Key.Grupo, r.Key.Moneda, r.Value, Total("importe_mes", r.Key.Grupo, r.Key.Moneda))).ToArray());
    }

    private static async Task<Dictionary<string, DateTimeOffset>> Fechas(NpgsqlConnection db, NpgsqlTransaction tx, CancellationToken ct)
    {
        var fuentes = new Dictionary<string, DateTimeOffset>();
        await using var cmd = Comando(db, tx, "SELECT fuente,actualizado_en FROM global.sincronizacion");
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        while (await lector.ReadAsync(ct)) fuentes.Add(lector.GetString(0), lector.GetFieldValue<DateTimeOffset>(1));
        return fuentes;
    }

    public sealed record OfertaGlobal(Guid Id, Guid InmuebleId, string Nombre, string Ciudad, string Tipo, string Codigo,
        long Unidades, long Disponibles, decimal Precio, string Moneda, DateTimeOffset PublicadaEn, string? Emisor, string? Red);

    public sealed record FuenteGlobal(string Fuente, DateTimeOffset? ActualizadoEn);
    public sealed record ImporteGlobal(string Moneda, decimal Importe);
    public sealed record MesGlobal(string MesRegistro, string Moneda, decimal ComprasConfirmadas, decimal Importe);
    public sealed record InformeGlobal(DateTimeOffset GeneradoEn, bool CargaInicialCompleta, FuenteGlobal[] Fuentes,
        decimal TenantsRegistrados, decimal TenantsActivos, decimal InmueblesRegistrados, decimal InmueblesBorrador,
        decimal InmueblesEnRevision, decimal InmueblesObservados, decimal InmueblesAprobados, decimal OfertasRegistradas,
        decimal TokensEmitidos, decimal TokensOfrecidos, decimal TokensVendidos, decimal? PorcentajeColocado,
        decimal ComprasPendientes, decimal ComprasConfirmadas, decimal ComprasFallidas, decimal ComprasCanceladas,
        ImporteGlobal[] ImportesConfirmados, MesGlobal[] ComprasMensuales);
}
