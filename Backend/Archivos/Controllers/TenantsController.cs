using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Archivos.Controllers
{
    [Route("interno/tenants")]
    [ApiController]
    public class TenantsController(IConfiguration configuration) : ControllerBase
    {
        [HttpPost("{tenantId}/preparar")]
        public async Task<IActionResult> Preparar(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
            {
                return BadRequest("El tenant no es valido.");
            }

            await ArchivosContext
                .PrepararTenantAsync(tenantId, configuration);

            return NoContent();
        }

        [HttpDelete("{tenantId:guid}/base")]
        public async Task<IActionResult> EliminarBase(Guid tenantId, CancellationToken ct)
        {
            if (tenantId == Guid.Empty) return BadRequest("Tenant inválido.");
            await ArchivosContext.EliminarTenantAsync(tenantId, configuration, ct);
            return NoContent();
        }

    }

}