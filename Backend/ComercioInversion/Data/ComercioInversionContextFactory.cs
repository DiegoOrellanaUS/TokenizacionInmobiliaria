using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ComercioInversion.Data
{
    public class ComercioInversionContextFactory : IDesignTimeDbContextFactory<ComercioInversionContext>
    {
        public ComercioInversionContext CreateDbContext(string[] args)
        {
            var directory = Path.GetDirectoryName(typeof(ComercioInversionContext).Assembly.Location)!;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<ComercioInversionContext>()
                .AddEnvironmentVariables()
                .Build();

            var connection = configuration.GetConnectionString("ComercioInversionContext");
            var options = new DbContextOptionsBuilder<ComercioInversionContext>()
                .UseNpgsql(connection)
                .Options;

            return new ComercioInversionContext(options);
        }
    }
}
