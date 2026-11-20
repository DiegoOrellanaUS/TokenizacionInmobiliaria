using System.Collections.Concurrent;
using InmueblesOperacion.Data;
using Microsoft.EntityFrameworkCore;

namespace InmueblesOperacion.Multitenancy;

public sealed class PrepararSilo
{
    private readonly ConcurrentDictionary<Guid, byte> preparados = new();
    private readonly SemaphoreSlim bloqueo = new(1);

    public async Task EjecutarAsync(Guid tenantId, InmueblesOperacionContext context, CancellationToken ct)
    {
        if (preparados.ContainsKey(tenantId))
            return;

        await bloqueo.WaitAsync(ct);
        try
        {
            if (!preparados.ContainsKey(tenantId))
            {
                await context.Database.MigrateAsync(ct);
                preparados.TryAdd(tenantId, 0);
            }
        }
        finally
        {
            bloqueo.Release();
        }
    }
}
