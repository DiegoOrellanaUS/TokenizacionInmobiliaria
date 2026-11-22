using System.Collections.Concurrent;
using Npgsql;

namespace Reportes.Data;

public sealed class SiloKpi(IConfiguration config)
{
    private const string Esquema = """
        CREATE SCHEMA IF NOT EXISTS reportes;
        CREATE TABLE IF NOT EXISTS reportes.kpi_evento (
            fuente text NOT NULL, evento_id bigint NOT NULL,
            procesado_en timestamptz NOT NULL DEFAULT now(),
            PRIMARY KEY (fuente, evento_id)
        );
        CREATE TABLE IF NOT EXISTS reportes.kpi_entidad (
            tipo text NOT NULL, referencia uuid NOT NULL, evento_id bigint NOT NULL, datos jsonb NOT NULL,
            PRIMARY KEY (tipo, referencia)
        );
        CREATE TABLE IF NOT EXISTS reportes.kpi_total (
            indicador text NOT NULL, grupo text NOT NULL, moneda text NOT NULL,
            valor numeric NOT NULL CHECK (valor >= 0), PRIMARY KEY (indicador, grupo, moneda)
        );
        CREATE TABLE IF NOT EXISTS reportes.kpi_fuente (
            fuente text PRIMARY KEY, sincronizado_en timestamptz NOT NULL
        );
        """;

    private readonly ConcurrentDictionary<Guid, byte> listos = new();

    public NpgsqlConnection Conexion(Guid tenant) => new(Cadena(tenant));

    public async Task Preparar(Guid tenant, CancellationToken ct)
    {
        if (listos.ContainsKey(tenant)) return;

        await using var mantenimiento = new NpgsqlConnection(Cadena(null));
        await mantenimiento.OpenAsync(ct);
        await Bloquear(mantenimiento, tenant, ct);
        if (!listos.ContainsKey(tenant))
        {
            var nombre = Nombre(tenant);
            await using var existe = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = $1)", mantenimiento);
            existe.Parameters.AddWithValue(nombre);
            if (await existe.ExecuteScalarAsync(ct) is false)
            {
                await using var crear = new NpgsqlCommand($"CREATE DATABASE \"{nombre}\"", mantenimiento);
                await crear.ExecuteNonQueryAsync(ct);
            }

            await using var db = Conexion(tenant);
            await db.OpenAsync(ct);
            await using var tx = await db.BeginTransactionAsync(ct);
            await using var esquema = new NpgsqlCommand(Esquema, db, tx);
            await esquema.ExecuteNonQueryAsync(ct);
            await tx.CommitAsync(ct);
            listos.TryAdd(tenant, 0);
        }
    }

    public async Task Eliminar(Guid tenant, CancellationToken ct)
    {
        await using var mantenimiento = new NpgsqlConnection(Cadena(null));
        await mantenimiento.OpenAsync(ct);
        await Bloquear(mantenimiento, tenant, ct);
        listos.TryRemove(tenant, out _);
        using var conexion = Conexion(tenant);
        NpgsqlConnection.ClearPool(conexion);
        await using var borrar = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{Nombre(tenant)}\" WITH (FORCE)", mantenimiento);
        await borrar.ExecuteNonQueryAsync(ct);
    }

    private string Cadena(Guid? tenant)
    {
        var configurada = config.GetConnectionString("ReportesContext");
        if (string.IsNullOrWhiteSpace(configurada))
            throw new InvalidOperationException("Configura ConnectionStrings:ReportesContext.");
        return new NpgsqlConnectionStringBuilder(configurada)
        {
            Database = tenant is { } id ? Nombre(id) : config["Reportes:BaseMantenimiento"] ?? "postgres",
            Pooling = tenant.HasValue
        }.ConnectionString;
    }

    private static string Nombre(Guid tenant) => $"reportes_{tenant:N}";

    private static async Task Bloquear(NpgsqlConnection db, Guid tenant, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT pg_advisory_lock(hashtextextended($1, 0))", db);
        command.Parameters.AddWithValue(Nombre(tenant));
        await command.ExecuteNonQueryAsync(ct);
    }
}
