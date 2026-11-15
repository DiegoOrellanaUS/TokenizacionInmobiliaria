using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tokenizacion.Data
{
    public class TokenizacionContextFactory : IDesignTimeDbContextFactory<TokenizacionContext>
    {
        public TokenizacionContext CreateDbContext(string[] args)
        {
            var directory = Path.GetDirectoryName(typeof(TokenizacionContext).Assembly.Location)!;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(directory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connection = configuration.GetConnectionString("TokenizacionContext");
            var options = new DbContextOptionsBuilder<TokenizacionContext>()
                .UseNpgsql(connection)
                .Options;

            return new TokenizacionContext(options);
        }
    }
}
