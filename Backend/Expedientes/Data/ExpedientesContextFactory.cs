using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Expedientes.Data
{
    public class ExpedientesContextFactory:IDesignTimeDbContextFactory<ExpedientesContext>
    {
        public ExpedientesContext CreateDbContext(string[] args)
        {
            var directory = Path.GetDirectoryName(
                typeof(ExpedientesContext).Assembly.Location)!;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connection = configuration.GetConnectionString("ExpedientesContext");

            var options = new DbContextOptionsBuilder<ExpedientesContext>()
                .UseNpgsql(connection)
                .Options;

            return new ExpedientesContext(options);
        }
    }
}
