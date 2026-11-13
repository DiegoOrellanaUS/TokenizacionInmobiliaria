using Expedientes.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddDbContext<ExpedientesContext>((services, options) =>
{
    var tenant = services.GetRequiredService<TenantContext>();
    if (tenant.TenantId == Guid.Empty)
        throw new InvalidOperationException("No hay un tenant seleccionado.");

    var connection = builder.Configuration.GetConnectionString("ExpedientesContext");

    var connectionBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connection);
    connectionBuilder.Database = $"archivos_expedientes_{tenant.TenantId:N}";
    options.UseNpgsql(connectionBuilder.ConnectionString);
});

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOpenApi();


builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<TenantHeaderHandler>();

builder.Services.AddHttpClient("Archivos", client =>
{
    client.BaseAddress = new Uri("https://localhost:7043/");
})
.AddHttpMessageHandler<TenantHeaderHandler>();

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

