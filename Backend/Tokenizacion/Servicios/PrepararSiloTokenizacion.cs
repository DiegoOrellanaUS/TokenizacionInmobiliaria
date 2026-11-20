using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Tokenizacion.Data;

namespace Tokenizacion.Servicios;

public sealed class PrepararSiloTokenizacion
{
    private readonly ConcurrentDictionary<Guid, byte> migrados = new();
    private readonly SemaphoreSlim bloqueo = new(1);

    public async Task EliminarAsync(Guid tenantId, TokenizacionContext context, CancellationToken ct)
    {
        await bloqueo.WaitAsync(ct);
        try
        {
            migrados.TryRemove(tenantId, out _);
            await context.Database.EnsureDeletedAsync(ct);
        }
        finally
        {
            bloqueo.Release();
        }
    }

    public async Task EjecutarAsync(Guid tenantId, TokenizacionContext context, CancellationToken ct)
    {
        if (migrados.ContainsKey(tenantId))
            return;

        await bloqueo.WaitAsync(ct);
        try
        {
            if (!migrados.ContainsKey(tenantId))
            {
                await context.Database.MigrateAsync(ct);
                migrados.TryAdd(tenantId, 0);
            }
        }
        finally { bloqueo.Release(); }
    }
}
