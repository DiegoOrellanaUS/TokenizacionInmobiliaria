namespace Expedientes.Multitenancy
{
    public class TenantHeaderHandler(IHttpContextAccessor accessor)
    : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var http = accessor.HttpContext
                ?? throw new InvalidOperationException(
                    "No hay una peticion actual.");

            var tenant = http.RequestServices
                .GetRequiredService<TenantContext>();

            if (tenant.TenantId == Guid.Empty)
            {
                throw new InvalidOperationException("No hay un tenant seleccionado.");
            }

            request.Headers.Remove("X-Tenant");
            request.Headers.Add("X-Tenant", tenant.TenantId.ToString("D"));

            return base.SendAsync(request, cancellationToken);
        }
    }
}