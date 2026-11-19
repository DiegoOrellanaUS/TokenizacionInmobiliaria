using Adminstracion.Data;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Adminstracion.Dominio;
using Adminstracion.Servicios;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ArchivosMultitenenciaAdministracionContext");

builder.Services.AddDbContext<AdminContext>(options =>
    options.UseNpgsql(connectionString, postgres => postgres.MigrationsHistoryTable("__AdminMigrationsHistory")));
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient("AdministracionInterna", client => client.Timeout = TimeSpan.FromSeconds(45));
builder.Services.AddScoped<AprovisionarTenant>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
