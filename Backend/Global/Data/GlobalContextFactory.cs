using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Global.Data
{
    public class GlobalContextFactory : IDesignTimeDbContextFactory<GlobalContext>
    {
        public GlobalContext CreateDbContext(string[] args)
        {
            var directory = Path.GetDirectoryName(typeof(GlobalContext).Assembly.Location)!;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connection = configuration.GetConnectionString("GlobalContext");
            var options = new DbContextOptionsBuilder<GlobalContext>()
                .UseNpgsql(connection)
                .Options;

            return new GlobalContext(options);
        }
    }
}
