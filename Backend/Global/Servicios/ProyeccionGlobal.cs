using System.Globalization;
using System.Text.Json;
using Global.Data;
using Npgsql;
using static Global.Data.AlmacenGlobal;

namespace Global.Servicios;

public sealed record EventoGlobal(long Id, Guid TenantId, string Tipo, Guid Referencia, JsonElement Datos);
public sealed record TenantGlobal(Guid Id, bool Publicable);

public sealed class ProyeccionGlobal(AlmacenGlobal almacen)
{
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");

    public async Task Aplicar(string fuente, EventoGlobal[] eventos, Guid? tenantEsperado, CancellationToken ct)
    {
        await using var db = almacen.Conexion();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Serializar(db, tx, ct);

        foreach (var evento in eventos.OrderBy(e => e.Id))
        {
            var tipoValido = fuente switch
            {
                "inmuebles" => evento.Tipo == "inmueble",
                "tokenizacion" => evento.Tipo == "emision",
                "comercio" => evento.Tipo is "oferta" or "compra",
                _ => false
            };
            if (!tipoValido || evento.Id <= 0 || evento.TenantId == Guid.Empty || evento.Referencia == Guid.Empty ||
                tenantEsperado is { } id && id != evento.TenantId)
                throw new InvalidDataException("Evento inesperado en la proyección global.");

            if (await Ejecutar(db, tx, """
                INSERT INTO global.kpi_evento(tenant_id, fuente, evento_id) VALUES ($1, $2, $3)
                ON CONFLICT DO NOTHING
                """, ct, evento.TenantId, fuente, evento.Id) == 0)
                continue;

            using var anterior = await Anterior(db, tx, evento, ct);
            if (anterior is not null && anterior.RootElement.GetProperty("id").GetInt64() >= evento.Id) continue;
            var previo = anterior?.RootElement.GetProperty("datos") ?? default;
            var cambios = Aportes(evento.Tipo, previo).Select(a => a with { Valor = -a.Valor })
                .Concat(Aportes(evento.Tipo, evento.Datos))
                .GroupBy(a => (a.Indicador, a.Grupo, a.Moneda));

            foreach (var grupo in cambios)
            {
                var delta = grupo.Sum(a => a.Valor);
                if (delta == 0) continue;
                var clave = grupo.Key;
                await Ejecutar(db, tx, """
                    INSERT INTO global.kpi_total VALUES ($1,$2,$3,0) ON CONFLICT DO NOTHING
                    """, ct, clave.Indicador, clave.Grupo, clave.Moneda);
                await Ejecutar(db, tx, """
                    UPDATE global.kpi_total SET valor = valor + $4 WHERE indicador=$1 AND grupo=$2 AND moneda=$3
                    """, ct, clave.Indicador, clave.Grupo, clave.Moneda, delta);
            }

            await Ejecutar(db, tx, """
                INSERT INTO global.kpi_entidad VALUES ($1,$2,$3,$4,$5::jsonb)
                ON CONFLICT (tenant_id,tipo,referencia) DO UPDATE SET evento_id=EXCLUDED.evento_id, datos=EXCLUDED.datos
                """, ct, evento.TenantId, evento.Tipo, evento.Referencia, evento.Id, evento.Datos.GetRawText());

            if (evento.Tipo == "oferta") await AplicarOferta(db, tx, evento, ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task ActualizarTenants(IReadOnlyList<TenantGlobal> tenants, CancellationToken ct)
    {
        var activos = tenants.Where(t => t.Publicable).Select(t => t.Id).ToArray();
        await using var db = almacen.Conexion();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Serializar(db, tx, ct);
        await Ejecutar(db, tx, """
            UPDATE global.tenant_publicacion SET publicable=false WHERE publicable AND NOT (tenant_id=ANY($1))
            """, ct, activos);
        await Ejecutar(db, tx, """
            INSERT INTO global.tenant_publicacion SELECT id,true FROM unnest($1::uuid[]) AS t(id)
            ON CONFLICT (tenant_id) DO UPDATE SET publicable=true WHERE NOT global.tenant_publicacion.publicable
            """, ct, activos);
        await Ejecutar(db, tx, """
            INSERT INTO global.kpi_total VALUES ('tenants','total','',$1),('tenants','activos','',$2)
            ON CONFLICT (indicador,grupo,moneda) DO UPDATE SET valor=EXCLUDED.valor
            """, ct, tenants.Count, activos.Length);
        await Marcar(db, tx, "admin", ct);
        await tx.CommitAsync(ct);
    }

    public async Task Marcar(string fuente, CancellationToken ct)
    {
        await using var db = almacen.Conexion();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        await Marcar(db, tx, fuente, ct);
        await tx.CommitAsync(ct);
    }

    private static Task<int> Marcar(NpgsqlConnection db, NpgsqlTransaction tx, string fuente, CancellationToken ct) =>
        Ejecutar(db, tx, """
            INSERT INTO global.sincronizacion VALUES ($1,now())
            ON CONFLICT (fuente) DO UPDATE SET actualizado_en=EXCLUDED.actualizado_en
            """, ct, fuente);

    private static Task<int> Serializar(NpgsqlConnection db, NpgsqlTransaction tx, CancellationToken ct) =>
        Ejecutar(db, tx, "SELECT pg_advisory_xact_lock(82702432)", ct);

    private static async Task<JsonDocument?> Anterior(NpgsqlConnection db, NpgsqlTransaction tx, EventoGlobal evento, CancellationToken ct)
    {
        await using var cmd = Comando(db, tx, """
            SELECT jsonb_build_object('id',evento_id,'datos',datos)::text FROM global.kpi_entidad
            WHERE tenant_id=$1 AND tipo=$2 AND referencia=$3
            """, evento.TenantId, evento.Tipo, evento.Referencia);
        return await cmd.ExecuteScalarAsync(ct) is string json ? JsonDocument.Parse(json) : null;
    }

    private static async Task AplicarOferta(NpgsqlConnection db, NpgsqlTransaction tx, EventoGlobal evento, CancellationToken ct)
    {
        var datos = evento.Datos;
        if (Eliminado(datos))
        {
            await Ejecutar(db, tx, "DELETE FROM global.oferta_publica WHERE id=$1 AND tenant_id=$2",
                ct, evento.Referencia, evento.TenantId);
            return;
        }

        // Los eventos históricos anteriores al catálogo se completan con la instantánea de la migración de Comercio.
        if (!datos.TryGetProperty("publicadaEn", out var publicada)) return;
        await Ejecutar(db, tx, """
            INSERT INTO global.oferta_publica VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12)
            ON CONFLICT (id) DO UPDATE SET nombre=EXCLUDED.nombre, ciudad=EXCLUDED.ciudad, tipo=EXCLUDED.tipo,
                codigo=EXCLUDED.codigo, unidades=EXCLUDED.unidades, disponibles=EXCLUDED.disponibles,
                precio=EXCLUDED.precio, moneda=EXCLUDED.moneda, publicada_en=EXCLUDED.publicada_en
            WHERE global.oferta_publica.tenant_id=EXCLUDED.tenant_id
            """, ct, evento.Referencia, evento.TenantId, datos.GetProperty("inmuebleId").GetGuid(),
            Texto(datos, "nombre"), Texto(datos, "ciudad"), Texto(datos, "tipo"), Texto(datos, "codigo"),
            datos.GetProperty("unidades").GetInt64(), datos.GetProperty("disponibles").GetInt64(),
            Numero(datos, "precio"), Texto(datos, "moneda"), publicada.GetDateTimeOffset().ToUniversalTime());
    }

    private sealed record Aporte(string Indicador, string Grupo, string Moneda, decimal Valor);

    private static IEnumerable<Aporte> Aportes(string tipo, JsonElement datos)
    {
        if (datos.ValueKind == JsonValueKind.Undefined || Eliminado(datos)) yield break;
        switch (tipo)
        {
            case "inmueble":
                yield return new("inmuebles", Texto(datos, "estado"), "", 1);
                break;
            case "emision":
                if (Texto(datos, "estado") == "CONFIRMADA")
                    yield return new("tokens_emitidos", "", "", Numero(datos, "unidades"));
                break;
            case "oferta":
                yield return new("ofertas", "", "", 1);
                yield return new("tokens_ofrecidos", "", "", Numero(datos, "unidades"));
                break;
            case "compra":
                var estado = Texto(datos, "estado");
                yield return new("compras", estado, "", 1);
                if (estado != "CONFIRMADA") break;
                var moneda = Texto(datos, "moneda");
                var unidades = Numero(datos, "unidades");
                var importe = unidades * Numero(datos, "precio");
                var mes = TimeZoneInfo.ConvertTime(datos.GetProperty("fecha").GetDateTimeOffset(), Zona)
                    .ToString("yyyy-MM", CultureInfo.InvariantCulture);
                yield return new("tokens_vendidos", "", "", unidades);
                yield return new("importe_confirmado", "", moneda, importe);
                yield return new("compras_mes", mes, moneda, 1);
                yield return new("importe_mes", mes, moneda, importe);
                break;
        }
    }

    private static bool Eliminado(JsonElement datos) =>
        datos.TryGetProperty("eliminado", out var eliminado) && eliminado.GetBoolean();
    private static string Texto(JsonElement datos, string clave) => datos.GetProperty(clave).GetString()!;
    private static decimal Numero(JsonElement datos, string clave) => datos.GetProperty(clave).GetDecimal();
}
