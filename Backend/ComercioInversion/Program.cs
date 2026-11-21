using ComercioInversion.Data;
using ComercioInversion.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddDbContext<ComercioInversionContext>((services, options) =>
{
    var tenant = services.GetRequiredService<TenantContext>();
    if (tenant.TenantId == Guid.Empty)
        throw new InvalidOperationException("No hay un tenant seleccionado.");

    var connection = builder.Configuration.GetConnectionString("ComercioInversionContext");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Configura la conexión ComercioInversionContext.");

    var connectionBuilder = new NpgsqlConnectionStringBuilder(connection)
    {
        Database = $"comercio_inversion_{tenant.TenantId:N}"
    };
    options.UseNpgsql(connectionBuilder.ConnectionString);
});

builder.Services.AddDbContext<CatalogoContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CatalogoContext")));
builder.Services.AddSingleton<PrepararCatalogo>();
builder.Services.AddHostedService<LiquidarComprasDemo>();
builder.Services.AddHttpClient("AdminCompraDemo", client =>
{
    client.BaseAddress = new Uri((builder.Configuration["Comercio:AdminUrl"] ?? "https://localhost:7266").TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient("LiquidacionDemo", client =>
{
    client.BaseAddress = new Uri((builder.Configuration["Comercio:TokenizacionUrl"] ?? "https://localhost:7314").TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(90);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

foreach (var (nombre, clave, direccion) in new[]
{
    ("TokensOferta", "Comercio:TokenizacionUrl", "https://localhost:7314"),
    ("InmueblesOferta", "Comercio:InmueblesUrl", "https://localhost:7311")
})
{
    builder.Services.AddHttpClient(nombre, client =>
    {
        client.BaseAddress = new Uri((builder.Configuration[clave] ?? direccion).TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(12);
    }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsEnvironment("Piloto"))
    app.UseHttpsRedirection();
app.MapGet("/salud", () => Results.Ok());
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
