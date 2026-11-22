using Adminstracion.Data;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Adminstracion.Dominio;
using Adminstracion.Servicios;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ArchivosMultitenenciaAdministracionContext");

builder.Services.AddDbContext<AdminContext>(options =>
    options.UseNpgsql(connectionString, postgres => postgres.MigrationsHistoryTable("__AdminMigrationsHistory")));
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient("AdministracionInterna", client => client.Timeout = TimeSpan.FromSeconds(45))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<AprovisionarTenant>();
var proteccion = builder.Services.AddDataProtection();
if (builder.Environment.IsEnvironment("Piloto"))
{
    var rutaClaves = builder.Configuration["SesionDemo:DirectorioClaves"] ??
        throw new InvalidOperationException("Configura SesionDemo:DirectorioClaves en el volumen de Admin.");
    proteccion.PersistKeysToFileSystem(Directory.CreateDirectory(rutaClaves)).SetApplicationName("Umbral.Piloto");
}

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("cuentas", limite =>
    {
        limite.PermitLimit = 30;
        limite.Window = TimeSpan.FromMinutes(1);
    });
});
builder.Services.AddScoped<SesionDemo>();
builder.Services.AddScoped<SesionesOAuth>();
builder.Services.AddScoped<PerfilSesion>();
builder.Services.AddScoped<InvitacionesRevision>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

var app = builder.Build();

if (app.Environment.IsEnvironment("Piloto"))
{
    await InicializarPiloto.PrepararAsync(app);

}
else
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.MapGet("/salud", () => Results.Ok());
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
