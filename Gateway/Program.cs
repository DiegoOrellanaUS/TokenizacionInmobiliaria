using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .AddDocument("administracion", "Administracion",
                "/openapi/administracion.json")
            .AddDocument("archivos", "Archivos",
                "/openapi/archivos.json")
            .AddDocument("expedientes", "Expedientes",
                "/openapi/expedientes.json")
            .AddServer("https://localhost:7039", "Gateway");
    });
}

app.MapReverseProxy();

app.Run();


