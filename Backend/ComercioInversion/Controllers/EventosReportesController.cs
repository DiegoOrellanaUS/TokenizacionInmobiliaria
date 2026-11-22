using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ComercioInversion.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComercioInversion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/tenants/{tenantId:guid}/eventos-reportes")]
public sealed class EventosReportesController(CatalogoContext context, PrepararCatalogo silo,
    IConfiguration config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Pendientes(Guid tenantId, CancellationToken ct)
    {
        if (!Autorizado(tenantId)) return NotFound();
        await silo.Ejecutar(context, ct);
        var filas = await context.Database.SqlQuery<Fila>($"""
            SELECT id AS "Id", tipo AS "Tipo", referencia AS "Referencia", datos::text AS "Datos"
            FROM catalogo.evento_reportes
            WHERE tenant_id = {tenantId} AND NOT entregado ORDER BY id LIMIT 200
            """).ToListAsync(ct);
        return Ok(filas.Select(f => new { f.Id, f.Tipo, f.Referencia, Datos = JsonSerializer.Deserialize<JsonElement>(f.Datos) }));
    }

    [HttpPost("confirmar")]
    public async Task<IActionResult> Confirmar(Guid tenantId, long[] ids, CancellationToken ct)
    {
        if (!Autorizado(tenantId)) return NotFound();
        if (ids.Length is < 1 or > 200 || ids.Any(id => id <= 0)) return BadRequest();
        await silo.Ejecutar(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE catalogo.evento_reportes SET entregado = true
            WHERE tenant_id = {tenantId} AND id = ANY({ids}) AND NOT entregado
            """, ct);
        return NoContent();
    }

    private bool Autorizado(Guid tenantId)
    {
        var clave = config["Reportes:ClaveInterna"] ?? config["Aprovisionamiento:ClaveInterna"];
        return tenantId != Guid.Empty && !string.IsNullOrWhiteSpace(clave) &&
            Request.Headers.TryGetValue("X-Reportes-Interna", out var recibida) && recibida.Count == 1 &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString()));
    }

    private sealed record Fila(long Id, string Tipo, Guid Referencia, string Datos);
}
