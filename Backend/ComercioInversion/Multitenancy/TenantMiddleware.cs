namespace ComercioInversion.Multitenancy
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate next;
        public TenantMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        public async Task InvokeAsync(HttpContext http, TenantContext tenant)
        {
            if (!http.Request.Path.StartsWithSegments("/api") ||
                http.Request.Path.StartsWithSegments("/api/Compras") ||
                HttpMethods.IsGet(http.Request.Method) &&
                (http.Request.Path.StartsWithSegments("/api/Ofertas/Catalogo") ||
                 http.Request.Path.StartsWithSegments("/api/Ofertas/Buscar")))
            {
                await next(http);
                return;
            }

            var headers = http.Request.Headers;
            if (!headers.TryGetValue("X-Tenant", out var values)
                || values.Count != 1
                || !Guid.TryParse(values[0], out var tenantId)
                || tenantId == Guid.Empty)
            {
                http.Response.StatusCode = StatusCodes.Status400BadRequest;
                await http.Response.WriteAsync("Debes enviar un X-Tenant valido.");
                return;
            }

            tenant.TenantId = tenantId;
            await next(http);
        }
    }
}
