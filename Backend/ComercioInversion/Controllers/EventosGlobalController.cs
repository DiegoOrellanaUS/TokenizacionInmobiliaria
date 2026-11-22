using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ComercioInversion.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComercioInversion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/global/eventos-comercio")]
public sealed class EventosGlobalController(CatalogoContext context, PrepararCatalogo silo,
    IConfiguration config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Pendientes(CancellationToken ct)
    {
        if (!Autorizado()) return NotFound();
        await silo.Ejecutar(context, ct);
        var filas = await context.Database.SqlQuery<Fila>($"""
            SELECT id AS "Id", tenant_id AS "TenantId", tipo AS "Tipo", referencia AS "Referencia", datos::text AS "Datos"
            FROM catalogo.evento_reportes WHERE NOT entregado_global ORDER BY id LIMIT 200
            """).ToListAsync(ct);
        return Ok(filas.Select(f => new { f.Id, f.TenantId, f.Tipo, f.Referencia,
            Datos = JsonSerializer.Deserialize<JsonElement>(f.Datos) }));
    }

    [HttpPost("confirmar")]
    public async Task<IActionResult> Confirmar(long[] ids, CancellationToken ct)
    {
        if (!Autorizado()) return NotFound();
        if (ids.Length is < 1 or > 200 || ids.Any(id => id <= 0)) return BadRequest();
        await silo.Ejecutar(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE catalogo.evento_reportes SET entregado_global = true WHERE id = ANY({ids}) AND NOT entregado_global
            """, ct);
        return NoContent();
    }

    private bool Autorizado()
    {
        var clave = config["Global:ClaveEventos"];
        return !string.IsNullOrWhiteSpace(clave) &&
            Request.Headers.TryGetValue("X-Global-Eventos", out var recibida) && recibida.Count == 1 &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString()));
    }

    private sealed record Fila(long Id, Guid TenantId, string Tipo, Guid Referencia, string Datos);
}
