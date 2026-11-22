using Reportes.Data;
using Reportes.Multitenancy;
using Reportes.Servicios;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddDbContext<ReportesContext>((services, options) =>
{
    var tenant = services.GetRequiredService<TenantContext>();
    if (tenant.TenantId == Guid.Empty)
        throw new InvalidOperationException("No hay un tenant seleccionado.");

    var connection = builder.Configuration.GetConnectionString("ReportesContext");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Configura la conexión ReportesContext.");

    var connectionBuilder = new NpgsqlConnectionStringBuilder(connection)
    {
        Database = $"reportes_{tenant.TenantId:N}"
    };
    options.UseNpgsql(connectionBuilder.ConnectionString);
});

builder.Services.AddSingleton<SiloKpi>();
builder.Services.AddSingleton<ProyeccionKpi>();
builder.Services.AddSingleton<ConsultarKpi>();
builder.Services.AddHostedService<SincronizarKpi>();
foreach (var (nombre, clave, local) in new[]
{
    ("admin", "AdminUrl", "https://localhost:7266"),
    ("inmuebles", "InmueblesUrl", "https://localhost:7311"),
    ("tokenizacion", "TokenizacionUrl", "https://localhost:7314"),
    ("comercio", "ComercioUrl", "https://localhost:7313")
})
{
    builder.Services.AddHttpClient(nombre, cliente =>
    {
        cliente.BaseAddress = new Uri((builder.Configuration["Reportes:" + clave] ?? local).TrimEnd('/') + "/");
        cliente.Timeout = TimeSpan.FromSeconds(20);
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
