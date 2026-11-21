using Microsoft.EntityFrameworkCore;

namespace ComercioInversion.Data;

public sealed class PrepararCatalogo
{
    private readonly SemaphoreSlim bloqueo = new(1);
    private volatile bool listo;

    public async Task Ejecutar(CatalogoContext context, CancellationToken ct)
    {
        if (listo)
            return;
        await bloqueo.WaitAsync(ct);
        try
        {
            if (!listo)
            {
                await context.Database.MigrateAsync(ct);
                listo = true;
            }
        }
        finally { bloqueo.Release(); }
    }
}
