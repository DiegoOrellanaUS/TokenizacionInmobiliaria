using Global.Data;
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

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
