using Archivos.Dominio;
using Microsoft.EntityFrameworkCore;

public class ArchivosContext(DbContextOptions<ArchivosContext> options) : DbContext(options)
{
    public DbSet<Archivo> Archivo { get; set; } = default!;


    private static readonly SemaphoreSlim creationLock = new(1, 1);

    public static async Task PrepararTenantAsync(
        Guid tenantId, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("ArchivosMultitenenciaContext");

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        builder.Database = $"archivos_archivo_{tenantId:N}";

        var options = new DbContextOptionsBuilder<ArchivosContext>()
            .UseNpgsql(builder.ConnectionString).Options;

        await creationLock.WaitAsync();
        try
        {
            await using var context = new ArchivosContext(options);
            await context.Database.MigrateAsync();
        }
        finally
        {
            creationLock.Release();
        }
    }

    public static async Task EliminarTenantAsync(
        Guid tenantId, IConfiguration configuration, CancellationToken ct)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant inválido.", nameof(tenantId));

        var connection = configuration.GetConnectionString("ArchivosMultitenenciaContext")
            ?? throw new InvalidOperationException("Falta la conexión de Archivos.");
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connection)
        {
            Database = $"archivos_archivo_{tenantId:N}"
        };

        await creationLock.WaitAsync(ct);
        try
        {
            var options = new DbContextOptionsBuilder<ArchivosContext>()
                .UseNpgsql(builder.ConnectionString).Options;
            await using var context = new ArchivosContext(options);
            await context.Database.EnsureDeletedAsync(ct);
        }
        finally { creationLock.Release(); }
    }


}
