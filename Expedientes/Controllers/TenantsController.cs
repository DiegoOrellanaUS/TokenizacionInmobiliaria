using Microsoft.AspNetCore.Mvc;

namespace Expedientes.Controllers
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

            await ExpedientesContext
                .PrepararTenantAsync(tenantId, configuration);

            return NoContent();
        }
    }
}