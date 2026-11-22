using System.Net;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Reportes.Data;
using Reportes.Exportacion;
using Reportes.Multitenancy;
using Reportes.Servicios;

namespace Reportes.Controllers;

[ApiController]
[Route("api/Reportes/Kpi")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class KpiController(TenantContext tenant, SiloKpi silos, ConsultarKpi consulta,
    IHttpClientFactory clientes, IConfiguration config) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> Obtener(CancellationToken ct) => ConTitular(informe => Ok(informe), ct);

    [HttpGet("excel")]
    public Task<IActionResult> Excel(CancellationToken ct) => ConTitular(informe => informe.Listo
        ? File(ExcelKpi.Crear(InformeTenantExportacion.Crear(informe)), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "kpi-umbral.xlsx")
        : Conflict("La carga inicial está en curso."), ct);

    [HttpGet("pdf")]
    public Task<IActionResult> Pdf(CancellationToken ct) => ConTitular(informe => informe.Listo
        ? File(PdfKpi.Crear(InformeTenantExportacion.Crear(informe)), "application/pdf", "kpi-umbral.pdf")
        : Conflict("La carga inicial está en curso."), ct);

    private async Task<IActionResult> ConTitular(Func<InformeKpi, IActionResult> responder, CancellationToken ct)
    {
        if (Request.Headers.Authorization.Count != 1) return Unauthorized();
        var clave = config["Inmuebles:ClaveInterna"];
        if (string.IsNullOrWhiteSpace(clave)) return StatusCode(503, "Reportes todavía no está configurado.");

        try
        {
            var id = tenant.TenantId;
            using var client = clientes.CreateClient("admin");
            using var request = new HttpRequestMessage(HttpMethod.Get, $"interno/inmuebles/{id:D}/acceso");
            request.Headers.Add("X-Inmuebles-Interna", clave);
            request.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());
            using var acceso = await client.SendAsync(request, ct);
            if (acceso.StatusCode == HttpStatusCode.Unauthorized) return Unauthorized();
            if (acceso.StatusCode == HttpStatusCode.Forbidden) return StatusCode(403);
            acceso.EnsureSuccessStatusCode();
            if ((await acceso.Content.ReadFromJsonAsync<Titular>(ct))?.EsTitular != true) return StatusCode(403);

            await silos.Preparar(id, ct);
            return responder(await consulta.Obtener(id, ct));
        }
        catch (Exception ex) when (!ct.IsCancellationRequested && ex is HttpRequestException or NpgsqlException or OperationCanceledException)
        {
            return StatusCode(503, "Los indicadores no están disponibles temporalmente.");
        }
    }

    private sealed record Titular(bool EsTitular);
}
