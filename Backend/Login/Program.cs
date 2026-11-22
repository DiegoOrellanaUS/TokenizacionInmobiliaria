using System.Net.Http.Headers;
using Adminstracion.Controllers;
using Adminstracion.Data;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
GitHubOAuth.VerificarConfiguracion(builder.Configuration);
FacebookOAuth.VerificarConfiguracion(builder.Configuration);
if (GitHubOAuth.Habilitado(builder.Configuration) && FacebookOAuth.Habilitado(builder.Configuration) &&
    GitHubOAuth.Origen(builder.Configuration) != FacebookOAuth.Origen(builder.Configuration))
    throw new InvalidOperationException("GitHub y Facebook deben usar el mismo origen público de Umbral.");
var conexion = builder.Configuration.GetConnectionString("ArchivosMultitenenciaAdministracionContext");
builder.Services.AddDbContext<AdminContext>(options => options.UseNpgsql(conexion,
    postgres => postgres.MigrationsHistoryTable("__AdminMigrationsHistory")));
builder.Services.AddControllers().ConfigureApplicationPartManager(manager =>
{
    manager.ApplicationParts.Clear();
    manager.ApplicationParts.Add(new AssemblyPart(typeof(GitHubOAuthController).Assembly));
});
var proteccion = builder.Services.AddDataProtection().SetApplicationName("Umbral.Login");
if ((builder.Configuration["FacebookOAuth:DirectorioClaves"] ??
    builder.Configuration["GitHubOAuth:DirectorioClaves"]) is { Length: > 0 } ruta)
    proteccion.PersistKeysToFileSystem(Directory.CreateDirectory(ruta));

builder.Services.AddHttpClient("GitHubOAuth", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Umbral-Login/1.0");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient("FacebookOAuth", client =>
{
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
  .RemoveAllLoggers();
builder.Services.AddScoped<IdentidadesOAuth>();
builder.Services.AddScoped<GitHubOAuth>();
builder.Services.AddScoped<FacebookOAuth>();
builder.Services.AddScoped<SesionesOAuth>();
builder.Services.AddScoped<PerfilSesion>();
builder.Services.AddScoped<InvitacionesRevision>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("cuentas", limite =>
    {
        limite.PermitLimit = 60;
        limite.Window = TimeSpan.FromMinutes(1);
    });
});

var app = builder.Build();
app.UseRateLimiter();
app.MapGet("/salud", () => Results.Ok());
app.MapControllers();
app.Run();
