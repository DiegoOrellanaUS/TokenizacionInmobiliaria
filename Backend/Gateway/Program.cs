using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var origins = builder.Configuration.GetSection("Cors:FrontendOrigins").Get<string[]>() ??
    (builder.Environment.IsDevelopment() ? ["http://localhost:5173", "http://127.0.0.1:5173"] : []);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(origins)
        .WithMethods("GET", "POST", "PUT")
        .WithHeaders("Authorization", "Content-Type", "X-Tenant")));

var app = builder.Build();
var piloto = app.Environment.IsEnvironment("Piloto");

if (!piloto)
    app.UseHttpsRedirection();
app.MapGet("/salud", () => Results.Ok());
app.UseCors();

app.Use(async (context, next) =>
{
    if (piloto && !EsRutaPiloto(context.Request))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    if (!piloto && (context.Request.Path.StartsWithSegments("/api/Administrador/BorrarBases") ||
         context.Request.Path.StartsWithSegments("/api/SesionDemo")) &&
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
    app.UseStaticFiles();

    app.MapScalarApiReference(options =>
    {
        options
            .WithJavaScriptConfiguration("/scalar-config.js")
            .AddDocument("administracion", "Administracion",
                "/openapi/administracion.json")

            .AddDocument("tokenizacion", "Tokenizacion",
                "/openapi/tokenizacion.json")
            .AddDocument("inmuebles", "Inmuebles y Operación",
                "/openapi/inmuebles.json")
            .AddServer("https://localhost:7339", "Gateway");
    });
}

app.MapReverseProxy();

app.Run();

static bool EsRutaPiloto(HttpRequest request)
{
    var path = request.Path;
    if (path == "/salud") return HttpMethods.IsGet(request.Method);

    if (HttpMethods.IsPost(request.Method) &&
        (path == "/api/Usuarios/crear" || path == "/api/SesionDemo/Iniciar" || path == "/api/Subscripciones/crear"))
        return true;

    if (HttpMethods.IsGet(request.Method) &&
        (path == "/api/Planes/Listar" || path == "/api/SesionDemo/EspaciosRevision"))
        return true;

    return path.StartsWithSegments("/api/Inmuebles") ||
        path.StartsWithSegments("/api/Tokenizacion/Inmuebles") ||
        path.StartsWithSegments("/api/Ofertas") ||
        path.StartsWithSegments("/api/Compras");
}


