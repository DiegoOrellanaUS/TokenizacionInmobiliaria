using System.Collections.Concurrent;
using InmueblesOperacion.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InmueblesOperacion.Multitenancy;

public sealed class PrepararSilo
{
    private readonly ConcurrentDictionary<Guid, byte> preparados = new();
    private readonly SemaphoreSlim bloqueo = new(1);

    public async Task EliminarAsync(Guid tenantId, InmueblesOperacionContext context, CancellationToken ct)
    {
        await bloqueo.WaitAsync(ct);
        try
        {
            preparados.TryRemove(tenantId, out _);
            await context.Database.EnsureDeletedAsync(ct);
        }
        finally
        {
            bloqueo.Release();
        }
    }

    public async Task<bool> PrepararExistenteAsync(Guid tenantId, InmueblesOperacionContext context, CancellationToken ct)
    {
        await bloqueo.WaitAsync(ct);
        try
        {
            try { await context.Database.OpenConnectionAsync(ct); }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidCatalogName) { return false; }
            await context.Database.CloseConnectionAsync();
            await MigrarAsync(tenantId, context, ct);
            return true;
        }
        finally { bloqueo.Release(); }
    }

    public async Task EjecutarAsync(Guid tenantId, InmueblesOperacionContext context, CancellationToken ct)
    {
        if (preparados.ContainsKey(tenantId))
            return;

        await bloqueo.WaitAsync(ct);
        try
        {
            await MigrarAsync(tenantId, context, ct);
        }
        finally
        {
            bloqueo.Release();
        }
    }

    private async Task MigrarAsync(Guid tenantId, InmueblesOperacionContext context, CancellationToken ct)
    {
        if (preparados.ContainsKey(tenantId)) return;
        await context.Database.MigrateAsync(ct);
        preparados.TryAdd(tenantId, 0);
    }
}
