using InmueblesOperacion.Data;
using InmueblesOperacion.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddSingleton<PrepararSilo>();
builder.Services.AddHttpClient("AdminInmuebles", client =>
{
    var direccion = builder.Configuration["Inmuebles:AdminUrl"] ?? "https://localhost:7266/";
    client.BaseAddress = new Uri(direccion.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddDbContext<InmueblesOperacionContext>((services, options) =>
{
    var tenant = services.GetRequiredService<TenantContext>();
    if (tenant.TenantId == Guid.Empty)
        throw new InvalidOperationException("No hay un tenant seleccionado.");

    var connection = builder.Configuration.GetConnectionString("InmueblesOperacionContext");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Configura la conexión InmueblesOperacionContext.");

    var connectionBuilder = new NpgsqlConnectionStringBuilder(connection)
    {
        Database = $"inmuebles_operacion_{tenant.TenantId:N}"
    };
    options.UseNpgsql(connectionBuilder.ConnectionString);
});

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
