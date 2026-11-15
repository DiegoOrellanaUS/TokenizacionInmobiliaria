using CajaPagos.Data;
using CajaPagos.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddDbContext<CajaPagosContext>((services, options) =>
{
    var tenant = services.GetRequiredService<TenantContext>();
    if (tenant.TenantId == Guid.Empty)
        throw new InvalidOperationException("No hay un tenant seleccionado.");

    var connection = builder.Configuration.GetConnectionString("CajaPagosContext");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("Configura la conexión CajaPagosContext.");

    var connectionBuilder = new NpgsqlConnectionStringBuilder(connection)
    {
        Database = $"caja_pagos_{tenant.TenantId:N}"
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

app.UseHttpsRedirection();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
