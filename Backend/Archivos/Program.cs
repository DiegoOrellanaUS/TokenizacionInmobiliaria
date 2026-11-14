using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Archivos.Multitenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddDbContext<ArchivosContext>((services, options) =>
{
    var tenant = services.GetRequiredService<TenantContext>();
    if (tenant.TenantId == Guid.Empty)
        throw new InvalidOperationException("No hay un tenant seleccionado.");

    var connection = builder.Configuration.GetConnectionString("ArchivosMultitenenciaContext");

    var connectionBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connection);
    connectionBuilder.Database = $"archivos_archivo_{tenant.TenantId:N}";
    options.UseNpgsql(connectionBuilder.ConnectionString);
});

// Add services to the container.

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

