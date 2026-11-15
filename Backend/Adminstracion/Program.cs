using Adminstracion.Data;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Adminstracion.Dominio;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ArchivosMultitenenciaAdministracionContext");

builder.Services.AddDbContext<AdminstracionContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddDbContext<AdminContext>(options =>
    options.UseNpgsql(connectionString, postgres => postgres.MigrationsHistoryTable("__AdminMigrationsHistory")));
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

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
