using System.Net;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class AdministradorController(AprovisionarTenant aprovisionador, IWebHostEnvironment environment) : ControllerBase
{
    [HttpDelete("BorrarBases/{tenantId:guid}", Name = "BorrarBasesTenant")]
    public async Task<IActionResult> BorrarBases(Guid tenantId, CancellationToken ct)
    {
        if (!environment.IsDevelopment() ||
            HttpContext.Connection.RemoteIpAddress is not IPAddress remoto || !IPAddress.IsLoopback(remoto))
            return NotFound();

        if (tenantId == Guid.Empty)
            return BadRequest("Indica un TenantId válido.");

        var servicios = await aprovisionador.EliminarBasesTenantAsync(tenantId, ct);
        if (servicios == null)
            return NotFound("El tenant no existe.");

        var completas = servicios.Values.All(estado => estado == "BORRADA");
        var resultado = new { TenantId = tenantId, Estado = completas ? "BASES_BORRADAS" : "BORRADO_PARCIAL", Servicios = servicios };
        if (!completas)
            return StatusCode(StatusCodes.Status502BadGateway, resultado);

        return Ok(resultado);
    }
}
