using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tokenizacion.Data;
using Tokenizacion.DTOs;
using Tokenizacion.Multitenancy;
using Tokenizacion.Servicios;

namespace Tokenizacion.Controllers;

[ApiController]
[Route("api/Tokenizacion/Inmuebles/{inmuebleId:guid}")]
public class TokenizacionInmueblesController(TenantContext tenant, TokenizacionContext context, EmitirInmueble emisor,
    PrepararSiloTokenizacion silo, IHttpClientFactory clients, IConfiguration configuration) : ControllerBase
{
    [HttpPost("Emitir", Name = "EmitirTokenInmueble")]
    public Task<IActionResult> PostEmitir(Guid inmuebleId, TokenizarInmueble solicitud, CancellationToken ct) =>
        ConAccesoAsync(async acceso =>
        {
            var estado = await ConsultarInmuebleAsync(inmuebleId, ct);
            if (estado is null)
                return NotFound("No se encontró ese inmueble en el tenant.");
            if (estado != "APROBADO")
                return Conflict("El expediente debe estar aprobado para emitir el token.");

            await silo.EjecutarAsync(tenant.TenantId, context, ct);
            var resultado = await emisor.EjecutarAsync(tenant.TenantId, inmuebleId, acceso.UsuarioId,
                acceso.MaxTokensPropios, solicitud, ct);

            return resultado.Estado switch
            {
                "CONFIRMADA" => Ok(resultado),
                "FALLIDA" => Conflict(resultado),
                _ => AcceptedAtRoute("BuscarEmisionInmueble", new { inmuebleId }, resultado)
            };
        }, ct);

    [HttpGet("Buscar", Name = "BuscarEmisionInmueble")]
    public Task<IActionResult> GetBuscar(Guid inmuebleId, CancellationToken ct) =>
        ConAccesoAsync(async _ =>
        {
            await silo.EjecutarAsync(tenant.TenantId, context, ct);
            var resultado = await emisor.ConsultarAsync(inmuebleId, ct);
            return resultado is null ? NotFound() : Ok(resultado);
        }, ct);

    private async Task<IActionResult> ConAccesoAsync(Func<Acceso, Task<IActionResult>> operacion, CancellationToken ct)
    {
        var clave = configuration["Inmuebles:ClaveInterna"];
        if (string.IsNullOrWhiteSpace(clave))
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        if (Request.Headers.Authorization.Count != 1)
            return SolicitarCredenciales();

        try
        {
            using var client = clients.CreateClient("AdminTokenizacion");
            using var consulta = new HttpRequestMessage(HttpMethod.Get, $"interno/inmuebles/{tenant.TenantId:D}/acceso");
            consulta.Headers.Add("X-Inmuebles-Interna", clave);
            consulta.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());

            using var respuesta = await client.SendAsync(consulta, ct);
            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
                return SolicitarCredenciales();
            if (respuesta.StatusCode == HttpStatusCode.Forbidden)
                return StatusCode(StatusCodes.Status403Forbidden);

            respuesta.EnsureSuccessStatusCode();
            var acceso = await respuesta.Content.ReadFromJsonAsync<Acceso>(ct);
            if (acceso is null || acceso.UsuarioId == Guid.Empty)
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            if (!acceso.EsTitular)
                return StatusCode(StatusCodes.Status403Forbidden);

            return await operacion(acceso);
        }
        catch (StellarRequisitoException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or StellarCliException or TimeoutException or JsonException ||
                                  ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "La operación no pudo completarse ahora. Consulta el estado antes de repetir la misma solicitud.");
        }
    }

    private async Task<string?> ConsultarInmuebleAsync(Guid inmuebleId, CancellationToken ct)
    {
        var clave = configuration["Inmuebles:ClaveEventos"];
        if (string.IsNullOrWhiteSpace(clave))
            throw new HttpRequestException("Falta la configuración de Inmuebles.");

        using var client = clients.CreateClient("InmueblesTokenizacion");
        using var consulta = new HttpRequestMessage(HttpMethod.Get,
            $"interno/tenants/{tenant.TenantId:D}/inmuebles/{inmuebleId:D}/elegibilidad");
        consulta.Headers.Add("X-Inmuebles-Eventos", clave);
        using var respuesta = await client.SendAsync(consulta, ct);
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
            return null;

        respuesta.EnsureSuccessStatusCode();
        var ficha = await respuesta.Content.ReadFromJsonAsync<Ficha>(ct);
        if (ficha?.InmuebleId != inmuebleId)
            throw new HttpRequestException("La respuesta de Inmuebles no coincide con la solicitud.");

        return ficha.EstadoRevision;
    }

    private IActionResult SolicitarCredenciales()
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"Tokenizacion\", charset=\"UTF-8\"";
        return Unauthorized();
    }

    private sealed record Acceso(Guid UsuarioId, bool EsTitular, int MaxTokensPropios);
    private sealed record Ficha(Guid InmuebleId, string EstadoRevision);
}
