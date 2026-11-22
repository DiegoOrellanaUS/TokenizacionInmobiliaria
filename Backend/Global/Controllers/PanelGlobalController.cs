using System.Net;
using Global.Data;
using Global.Servicios;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Reportes.Exportacion;

namespace Global.Controllers;

[ApiController]
[Route("api/Global/Panel")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PanelGlobalController(AlmacenGlobal almacen, ConsultarGlobal consulta,
    IHttpClientFactory clientes, IConfiguration config) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> Obtener(CancellationToken ct) => ConAcceso(panel => Ok(panel), ct);

    [HttpGet("excel")]
    public Task<IActionResult> Excel(CancellationToken ct) => ConAcceso(panel => panel.Listo
        ? File(ExcelKpi.Crear(panel.Exportar()), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "indicadores-globales-umbral.xlsx")
        : Conflict("La carga inicial de los indicadores globales está en curso."), ct);

    [HttpGet("pdf")]
    public Task<IActionResult> Pdf(CancellationToken ct) => ConAcceso(panel => panel.Listo
        ? File(PdfKpi.Crear(panel.Exportar()), "application/pdf", "indicadores-globales-umbral.pdf")
        : Conflict("La carga inicial de los indicadores globales está en curso."), ct);

    private async Task<IActionResult> ConAcceso(Func<InformePanelGlobal, IActionResult> responder, CancellationToken ct)
    {
        if (Request.Headers.Authorization.Count != 1) return Unauthorized();
        var clave = config["Global:ClaveAccesoPanel"];
        if (string.IsNullOrWhiteSpace(clave) || !almacen.Configurado)
            return StatusCode(503, "El panel global todavía no está configurado.");

        try
        {
            using var cliente = clientes.CreateClient("admin");
            using var solicitud = new HttpRequestMessage(HttpMethod.Get, "interno/global/acceso-panel");
            solicitud.Headers.Add("X-Global-Acceso", clave);
            solicitud.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());
            using var respuesta = await cliente.SendAsync(solicitud, ct);
            if (respuesta.StatusCode == HttpStatusCode.Unauthorized) return Unauthorized();
            if (respuesta.StatusCode == HttpStatusCode.Forbidden) return StatusCode(403, "Tu cuenta no tiene acceso al panel global.");
            if (respuesta.StatusCode != HttpStatusCode.NoContent)
                return StatusCode(503, "No se pudo verificar el acceso al panel global.");

            await almacen.Preparar(ct);
            return responder(InformePanelGlobal.Crear(await consulta.Indicadores(ct)));
        }
        catch (Exception ex) when (!ct.IsCancellationRequested && ex is HttpRequestException or NpgsqlException or OperationCanceledException)
        {
            return StatusCode(503, "Los indicadores globales no están disponibles temporalmente.");
        }
    }
}
