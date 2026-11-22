using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tokenizacion.Data;
using Tokenizacion.Multitenancy;
using Tokenizacion.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tokenizacion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/tenants/{tenantId:guid}/eventos-global")]
public sealed class EventosGlobalController(TenantContext tenant, IServiceProvider services, PrepararSiloTokenizacion silo,
    IConfiguration config) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Pendientes(Guid tenantId, CancellationToken ct)
    {
        if (!Autorizado() || tenantId == Guid.Empty) return NotFound();
        tenant.TenantId = tenantId;
        var context = services.GetRequiredService<TokenizacionContext>();
        if (!await silo.PrepararExistenteAsync(tenantId, context, ct)) return NotFound();
        var filas = await context.Database.SqlQuery<Fila>($"""
            SELECT id AS "Id", {tenantId} AS "TenantId", tipo AS "Tipo", referencia AS "Referencia", datos::text AS "Datos"
            FROM tokenizacion.evento_reportes WHERE NOT entregado_global ORDER BY id LIMIT 200
            """).ToListAsync(ct);
        return Ok(filas.Select(f => new { f.Id, f.TenantId, f.Tipo, f.Referencia,
            Datos = JsonSerializer.Deserialize<JsonElement>(f.Datos) }));
    }

    [HttpPost("confirmar")]
    public async Task<IActionResult> Confirmar(Guid tenantId, long[] ids, CancellationToken ct)
    {
        if (!Autorizado() || tenantId == Guid.Empty) return NotFound();
        if (ids.Length is < 1 or > 200 || ids.Any(id => id <= 0)) return BadRequest();
        tenant.TenantId = tenantId;
        var context = services.GetRequiredService<TokenizacionContext>();
        if (!await silo.PrepararExistenteAsync(tenantId, context, ct)) return NotFound();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE tokenizacion.evento_reportes SET entregado_global = true WHERE id = ANY({ids}) AND NOT entregado_global
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
