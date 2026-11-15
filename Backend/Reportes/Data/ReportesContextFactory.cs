using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reportes.Data
{
    public class ReportesContextFactory : IDesignTimeDbContextFactory<ReportesContext>
    {
        public ReportesContext CreateDbContext(string[] args)
        {
            var directory = Path.GetDirectoryName(typeof(ReportesContext).Assembly.Location)!;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connection = configuration.GetConnectionString("ReportesContext");
            var options = new DbContextOptionsBuilder<ReportesContext>()
                .UseNpgsql(connection)
                .Options;

            return new ReportesContext(options);
        }
    }
}
