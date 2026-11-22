using System.Globalization;
using System.Text.Json;
using Npgsql;
using Reportes.Data;

namespace Reportes.Servicios;

public sealed record EventoKpi(long Id, string Tipo, Guid Referencia, JsonElement Datos);

public sealed class ProyeccionKpi(SiloKpi silos)
{
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
    private sealed record Aporte(string Indicador, string Grupo, string Moneda, decimal Valor);

    public async Task Aplicar(Guid tenant, string fuente, IReadOnlyList<EventoKpi> eventos, bool alDia, CancellationToken ct)
    {
        await using var db = silos.Conexion(tenant);
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Ejecutar(db, tx, "SELECT pg_advisory_xact_lock(53530125)", ct);
        foreach (var evento in eventos.OrderBy(e => e.Id))
        {
            var esperado = fuente switch
            {
                "inmuebles" => evento.Tipo == "inmueble",
                "tokenizacion" => evento.Tipo == "emision",
                "comercio" => evento.Tipo is "compra" or "oferta",
                _ => false
            };
            if (!esperado || evento.Id <= 0 || evento.Referencia == Guid.Empty)
                throw new InvalidDataException("El evento no corresponde a la fuente.");

            var nuevo = await Ejecutar(db, tx, """
                INSERT INTO reportes.kpi_evento(fuente, evento_id) VALUES ($1, $2) ON CONFLICT DO NOTHING
                """, ct, fuente, evento.Id);
            if (nuevo == 0) continue;

            using var anterior = await Buscar(db, tx, evento, ct);
            // Un lote retrasado se puede confirmar sin volver a aplicar un estado anterior.
            if (anterior is null)
            {
                await Actualizar(db, tx, evento, ct);
            }
            else if (anterior.RootElement.GetProperty("id").GetInt64() < evento.Id)
            {
                var previo = anterior.RootElement.GetProperty("datos");
                await Actualizar(db, tx, evento, ct, previo);
            }
        }

        if (alDia)
            await Ejecutar(db, tx, """
                INSERT INTO reportes.kpi_fuente VALUES ($1, now())
                ON CONFLICT (fuente) DO UPDATE SET sincronizado_en = EXCLUDED.sincronizado_en
                """, ct, fuente);
        await tx.CommitAsync(ct);
    }

    private static async Task<JsonDocument?> Buscar(NpgsqlConnection db, NpgsqlTransaction tx, EventoKpi evento, CancellationToken ct)
    {
        await using var command = Comando(db, tx, """
            SELECT jsonb_build_object('id', evento_id, 'datos', datos)::text
            FROM reportes.kpi_entidad WHERE tipo = $1 AND referencia = $2
            """, evento.Tipo, evento.Referencia);
        return await command.ExecuteScalarAsync(ct) is string json ? JsonDocument.Parse(json) : null;
    }

    private static async Task Actualizar(NpgsqlConnection db, NpgsqlTransaction tx, EventoKpi evento,
        CancellationToken ct, JsonElement anterior = default)
    {
        var deltas = Aportes(evento, anterior).Select(a => a with { Valor = -a.Valor })
            .Concat(Aportes(evento, evento.Datos))
            .GroupBy(a => (a.Indicador, a.Grupo, a.Moneda))
            .Select(g => (g.Key, Delta: g.Sum(a => a.Valor)));
        foreach (var (clave, delta) in deltas)
        {
            if (delta == 0) continue;
            // Inserta una fila en cero y aplica el delta dentro de la transacción del evento.
            await Ejecutar(db, tx, """
                INSERT INTO reportes.kpi_total VALUES ($1, $2, $3, 0) ON CONFLICT DO NOTHING
                """, ct, clave.Indicador, clave.Grupo, clave.Moneda);
            await Ejecutar(db, tx, """
                UPDATE reportes.kpi_total SET valor = valor + $4 WHERE indicador = $1 AND grupo = $2 AND moneda = $3
                """, ct, clave.Indicador, clave.Grupo, clave.Moneda, delta);
        }

        await Ejecutar(db, tx, """
            INSERT INTO reportes.kpi_entidad VALUES ($1, $2, $3, $4::jsonb)
            ON CONFLICT (tipo, referencia) DO UPDATE SET evento_id = EXCLUDED.evento_id, datos = EXCLUDED.datos
            """, ct, evento.Tipo, evento.Referencia, evento.Id, evento.Datos.GetRawText());
    }

    private static IEnumerable<Aporte> Aportes(EventoKpi evento, JsonElement datos)
    {
        if (datos.ValueKind == JsonValueKind.Undefined ||
            datos.TryGetProperty("eliminado", out var eliminado) && eliminado.GetBoolean())
            yield break;

        switch (evento.Tipo)
        {
            case "inmueble":
                yield return new("inmuebles", Texto(datos, "estado"), "", 1);
                break;
            case "emision":
                if (Texto(datos, "estado") == "CONFIRMADA")
                    yield return new("emitidas", "", "", Numero(datos, "unidades"));
                break;
            case "oferta":
                yield return new("ofrecidas", "", "", Numero(datos, "unidades"));
                break;
            case "compra":
                var estado = Texto(datos, "estado");
                yield return new("compras", estado, "", 1);
                if (estado != "CONFIRMADA") break;
                var oferta = datos.GetProperty("ofertaId").GetGuid().ToString("D");
                var moneda = Texto(datos, "moneda");
                var unidades = Numero(datos, "unidades");
                var importe = unidades * Numero(datos, "precio");
                var mes = TimeZoneInfo.ConvertTime(datos.GetProperty("fecha").GetDateTimeOffset(), Zona)
                    .ToString("yyyy-MM", CultureInfo.InvariantCulture);
                yield return new("vendidas", "", "", unidades);
                yield return new("monto", "", moneda, importe);
                yield return new("unidades_moneda", "", moneda, unidades);
                yield return new("vendidas_oferta", oferta, "", unidades);
                yield return new("monto_oferta", oferta, moneda, importe);
                yield return new("importe_mes", mes, moneda, importe);
                yield return new("compras_mes", mes, moneda, 1);
                break;
        }
    }

    internal static string Texto(JsonElement datos, string campo) => datos.GetProperty(campo).GetString()!;
    internal static decimal Numero(JsonElement datos, string campo) => datos.GetProperty(campo).GetDecimal();

    private static NpgsqlCommand Comando(NpgsqlConnection db, NpgsqlTransaction tx, string sql, params object[] args)
    {
        var command = new NpgsqlCommand(sql, db, tx);
        foreach (var arg in args) command.Parameters.AddWithValue(arg);
        return command;
    }

    private static async Task<int> Ejecutar(NpgsqlConnection db, NpgsqlTransaction tx, string sql, CancellationToken ct, params object[] args)
    {
        await using var command = Comando(db, tx, sql, args);
        return await command.ExecuteNonQueryAsync(ct);
    }
}
