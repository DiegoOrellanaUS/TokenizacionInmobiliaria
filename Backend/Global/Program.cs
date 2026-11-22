using Global.Data;
using Global.Servicios;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GlobalContext>(options =>
{
    var connection = builder.Configuration.GetConnectionString("GlobalContext");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Configura la conexión GlobalContext.");

    options.UseNpgsql(connection);
});

builder.Services.AddSingleton<AlmacenGlobal>();
builder.Services.AddSingleton<ProyeccionGlobal>();
builder.Services.AddSingleton<ConsultarGlobal>();
builder.Services.AddHostedService<SincronizarGlobal>();
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
        cliente.BaseAddress = new Uri((builder.Configuration["Global:" + clave] ?? local).TrimEnd('/') + "/");
        cliente.Timeout = TimeSpan.FromSeconds(15);
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
app.UseAuthorization();

app.MapControllers();

app.Run();
