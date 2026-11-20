using Microsoft.AspNetCore.Mvc;
using Tokenizacion.Multitenancy;

namespace Tokenizacion.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TokenizacionController(TenantContext tenant) : ControllerBase
{
    [HttpGet("Estado", Name = "EstadoTokenizacion")]
    public IActionResult GetEstado()
    {
        return Ok(new { Servicio = "Tokenizacion", TenantId = tenant.TenantId });
    }
}
