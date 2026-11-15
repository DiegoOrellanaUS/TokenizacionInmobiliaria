using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CajaPagos.Data
{
    public class CajaPagosContextFactory : IDesignTimeDbContextFactory<CajaPagosContext>
    {
        public CajaPagosContext CreateDbContext(string[] args)
        {
            var directory = Path.GetDirectoryName(typeof(CajaPagosContext).Assembly.Location)!;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connection = configuration.GetConnectionString("CajaPagosContext");
            var options = new DbContextOptionsBuilder<CajaPagosContext>()
                .UseNpgsql(connection)
                .Options;

            return new CajaPagosContext(options);
        }
    }
}
