using System.Security.Cryptography;
using System.Text;
using Global.Data;
using Global.Servicios;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Global.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GlobalController(AlmacenGlobal almacen, ConsultarGlobal consulta, IConfiguration config) : ControllerBase
{
    [HttpGet("/api/Ofertas/Catalogo")]
    [HttpGet("/api/Global/Catalogo")]
    public Task<IActionResult> Catalogo(CancellationToken ct) => ConDatos(async () =>
    {
        var ofertas = await consulta.Catalogo(ct);
        return ofertas is null ? StatusCode(503, "El catálogo se está sincronizando.") : Ok(ofertas);
    }, ct);

    [HttpGet("/api/Global/Activos/{ofertaId:guid}")]
    public Task<IActionResult> Activo(Guid ofertaId, CancellationToken ct) => ConDatos(async () =>
    {
        if (ofertaId == Guid.Empty) return NotFound();
        var filas = await consulta.Catalogo(ct, ofertaId);
        if (filas is null) return StatusCode(503, "El catálogo se está sincronizando.");
        var oferta = filas.SingleOrDefault();
        return oferta is { Emisor: not null, Red: not null }
            ? Ok(new { oferta.Codigo, oferta.Emisor, oferta.Red }) : NotFound();
    }, ct);

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("/interno/global/indicadores")]
    public Task<IActionResult> Indicadores(CancellationToken ct)
    {
        var clave = config["Global:ClaveIndicadores"];
        if (string.IsNullOrWhiteSpace(clave) ||
            !Request.Headers.TryGetValue("X-Global-Indicadores", out var recibida) || recibida.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString())))
            return Task.FromResult<IActionResult>(NotFound());

        return ConDatos(async () => Ok(await consulta.Indicadores(ct)), ct);
    }

    private async Task<IActionResult> ConDatos(Func<Task<IActionResult>> ejecutar, CancellationToken ct)
    {
        if (!almacen.Configurado) return StatusCode(503, "Global todavía no está configurado.");
        try
        {
            await almacen.Preparar(ct);
            return await ejecutar();
        }
        catch (NpgsqlException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(503, "Los datos globales no están disponibles temporalmente.");
        }
    }
}
