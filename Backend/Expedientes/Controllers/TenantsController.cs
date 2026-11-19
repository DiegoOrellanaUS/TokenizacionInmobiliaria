using Microsoft.AspNetCore.Mvc;

namespace Expedientes.Controllers
{
    [Route("interno/tenants")]
    [ApiController]
    public class TenantsController(IConfiguration configuration, IWebHostEnvironment environment) : ControllerBase
    {
        [HttpPost("{tenantId}/preparar")]
        public async Task<IActionResult> Preparar(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
            {
                return BadRequest("El tenant no es valido.");
            }

            await ExpedientesContext
                .PrepararTenantAsync(tenantId, configuration);

            return NoContent();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpDelete("{tenantId:guid}/base")]
        public async Task<IActionResult> EliminarBase(Guid tenantId, CancellationToken ct)
        {
            if (!environment.IsDevelopment() ||
                HttpContext.Connection.RemoteIpAddress is not System.Net.IPAddress remoto || !System.Net.IPAddress.IsLoopback(remoto))
                return NotFound();

            if (tenantId == Guid.Empty)
                return BadRequest("Tenant inválido.");

            await ExpedientesContext.EliminarTenantAsync(tenantId, configuration, ct);
            return NoContent();
        }
    }
}
