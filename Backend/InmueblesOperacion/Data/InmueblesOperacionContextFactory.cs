using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InmueblesOperacion.Data
{
    public class InmueblesOperacionContextFactory : IDesignTimeDbContextFactory<InmueblesOperacionContext>
    {
        public InmueblesOperacionContext CreateDbContext(string[] args)
        {
            var directory = Path.GetDirectoryName(typeof(InmueblesOperacionContext).Assembly.Location)!;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<InmueblesOperacionContext>()
                .AddEnvironmentVariables()
                .Build();

            var connection = configuration.GetConnectionString("InmueblesOperacionContext");
            var options = new DbContextOptionsBuilder<InmueblesOperacionContext>()
                .UseNpgsql(connection)
                .Options;

            return new InmueblesOperacionContext(options);
        }
    }
}
