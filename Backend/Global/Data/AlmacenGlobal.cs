using Npgsql;

namespace Global.Data;

public sealed class AlmacenGlobal(IConfiguration config)
{
    private readonly SemaphoreSlim inicio = new(1);
    private volatile bool listo;

    public bool Configurado => !string.IsNullOrWhiteSpace(config.GetConnectionString("GlobalContext"));
    public NpgsqlConnection Conexion() => new(config.GetConnectionString("GlobalContext"));

    public async Task Preparar(CancellationToken ct)
    {
        if (listo) return;
        await inicio.WaitAsync(ct);
        try
        {
            if (listo) return;
            await using var db = Conexion();
            await db.OpenAsync(ct);
            await using var tx = await db.BeginTransactionAsync(ct);
            await Ejecutar(db, tx, "SELECT pg_advisory_xact_lock(82702431)", ct);
            await Ejecutar(db, tx, """
                CREATE SCHEMA IF NOT EXISTS global;
                CREATE TABLE IF NOT EXISTS global.kpi_evento (
                    tenant_id uuid NOT NULL, fuente text NOT NULL, evento_id bigint NOT NULL,
                    procesado_en timestamptz NOT NULL DEFAULT now(),
                    PRIMARY KEY (tenant_id, fuente, evento_id)
                );
                CREATE TABLE IF NOT EXISTS global.kpi_entidad (
                    tenant_id uuid NOT NULL, tipo text NOT NULL, referencia uuid NOT NULL,
                    evento_id bigint NOT NULL, datos jsonb NOT NULL,
                    PRIMARY KEY (tenant_id, tipo, referencia)
                );
                CREATE TABLE IF NOT EXISTS global.kpi_total (
                    indicador text NOT NULL, grupo text NOT NULL, moneda text NOT NULL,
                    valor numeric NOT NULL CHECK (valor >= 0), PRIMARY KEY (indicador, grupo, moneda)
                );
                CREATE TABLE IF NOT EXISTS global.tenant_publicacion (
                    tenant_id uuid PRIMARY KEY, publicable boolean NOT NULL
                );
                CREATE TABLE IF NOT EXISTS global.oferta_publica (
                    id uuid PRIMARY KEY, tenant_id uuid NOT NULL, inmueble_id uuid NOT NULL, nombre text NOT NULL,
                    ciudad text NOT NULL, tipo text NOT NULL, codigo text NOT NULL, unidades bigint NOT NULL,
                    disponibles bigint NOT NULL, precio numeric NOT NULL, moneda text NOT NULL, publicada_en timestamptz NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_global_oferta_fecha ON global.oferta_publica(publicada_en DESC,id);
                CREATE TABLE IF NOT EXISTS global.sincronizacion (
                    fuente text PRIMARY KEY, actualizado_en timestamptz NOT NULL
                );
                """, ct);
            await tx.CommitAsync(ct);
            listo = true;
        }
        finally { inicio.Release(); }
    }

    internal static NpgsqlCommand Comando(NpgsqlConnection db, NpgsqlTransaction? tx, string sql, params object[] valores)
    {
        var cmd = new NpgsqlCommand(sql, db, tx);
        foreach (var valor in valores) cmd.Parameters.AddWithValue(valor);
        return cmd;
    }

    internal static async Task<int> Ejecutar(NpgsqlConnection db, NpgsqlTransaction tx, string sql,
        CancellationToken ct, params object[] valores)
    {
        await using var cmd = Comando(db, tx, sql, valores);
        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
