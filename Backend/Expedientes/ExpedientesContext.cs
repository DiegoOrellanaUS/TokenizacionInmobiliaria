using Expedientes.Dominio;
using Microsoft.EntityFrameworkCore;

public class ExpedientesContext(DbContextOptions<ExpedientesContext> options) : DbContext(options)
{
    public DbSet<Empleado> Empleado { get; set; } = default!;
    public DbSet<EmpleadoArchivo> EmpleadoArchivo { get; set; } = default!;

    private static readonly SemaphoreSlim creationLock = new(1, 1);

    public static async Task PrepararTenantAsync(
        Guid tenantId, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("ExpedientesContext");

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connection);
        builder.Database = $"archivos_expedientes_{tenantId:N}";

        var options = new DbContextOptionsBuilder<ExpedientesContext>()
            .UseNpgsql(builder.ConnectionString).Options;

        await creationLock.WaitAsync();
        try
        {
            await using var context = new ExpedientesContext(options);
            await context.Database.MigrateAsync();
        }
        finally
        {
            creationLock.Release();
        }
    }

    public static async Task EliminarTenantAsync(Guid tenantId, IConfiguration configuration, CancellationToken ct)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant inválido.", nameof(tenantId));

        var connection = configuration.GetConnectionString("ExpedientesContext")
            ?? throw new InvalidOperationException("Falta la conexión de Expedientes.");
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connection)
        {
            Database = $"archivos_expedientes_{tenantId:N}"
        };
        var options = new DbContextOptionsBuilder<ExpedientesContext>()
            .UseNpgsql(builder.ConnectionString).Options;

        await creationLock.WaitAsync(ct);
        try
        {
            await using var context = new ExpedientesContext(options);
            await context.Database.EnsureDeletedAsync(ct);
        }
        finally
        {
            creationLock.Release();
        }
    }


}
