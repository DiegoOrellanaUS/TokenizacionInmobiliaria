using System.Security.Cryptography;
using System.Text;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace Adminstracion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccesoGlobalController(IConfiguration config, SesionDemo sesiones, PerfilSesion perfiles) : ControllerBase
{
    [HttpGet("/interno/global/acceso-panel")]
    public async Task<IActionResult> Autorizar(CancellationToken ct)
    {
        var clave = config["Global:ClaveAccesoPanel"];
        if (string.IsNullOrWhiteSpace(clave) ||
            !Request.Headers.TryGetValue("X-Global-Acceso", out var recibida) || recibida.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString())))
            return NotFound();

        if (await sesiones.LeerAsync(HttpContext, ct, exigirIdentidad: true) is not { } usuario)
            return Unauthorized();
        return await perfiles.EsAdminGlobal(usuario, ct) ? NoContent() : StatusCode(403);
    }
}
