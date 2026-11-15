using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Adminstracion.Data
{
    public class AdminContextFactory : IDesignTimeDbContextFactory<AdminContext>
    {
        public AdminContext CreateDbContext(string[] args)
        {
            var directory = Path.GetDirectoryName(typeof(AdminContext).Assembly.Location)!;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connection = configuration.GetConnectionString("ArchivosMultitenenciaAdministracionContext");
            var options = new DbContextOptionsBuilder<AdminContext>()
                .UseNpgsql(connection, postgres => postgres.MigrationsHistoryTable("__AdminMigrationsHistory"))
                .Options;

            return new AdminContext(options);
        }
    }
}
