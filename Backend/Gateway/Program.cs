using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/Administrador/BorrarBases") &&
        (!app.Environment.IsDevelopment() ||
         context.Connection.RemoteIpAddress is not System.Net.IPAddress remoto || !System.Net.IPAddress.IsLoopback(remoto)))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next(context);
});

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


