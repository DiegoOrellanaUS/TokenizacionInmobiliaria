using Tokenizacion.Data;
using Tokenizacion.Servicios;
using Tokenizacion.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddDbContext<TokenizacionContext>((services, options) =>
{
    var tenant = services.GetRequiredService<TenantContext>();
    if (tenant.TenantId == Guid.Empty)
        throw new InvalidOperationException("No hay un tenant seleccionado.");

    var connection = builder.Configuration.GetConnectionString("TokenizacionContext");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Configura la conexión TokenizacionContext.");

    var connectionBuilder = new NpgsqlConnectionStringBuilder(connection)
    {
        Database = $"tokenizacion_{tenant.TenantId:N}"
    };
    options.UseNpgsql(connectionBuilder.ConnectionString);
});

builder.Services.AddSingleton<StellarCli>();
builder.Services.AddSingleton<PrepararSiloTokenizacion>();
builder.Services.AddScoped<EmitirInmueble>();
builder.Services.AddHttpClient("AdminTokenizacion", client =>
{
    client.BaseAddress = new Uri((builder.Configuration["Tokenizacion:AdminUrl"] ?? "https://localhost:7266").TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient("InmueblesTokenizacion", client =>
{
    client.BaseAddress = new Uri((builder.Configuration["Tokenizacion:InmueblesUrl"] ?? "https://localhost:7311").TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient<RedStellar>(client =>
{
    client.BaseAddress = new Uri("https://soroban-testnet.stellar.org/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
