using System.Security.Cryptography;
using System.Text;
using InmueblesOperacion.Data;
using InmueblesOperacion.Dominio;
using InmueblesOperacion.Multitenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InmueblesOperacion.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("interno/tenants/{tenantId:guid}/inmuebles")]
public class EventosInmueblesController(TenantContext tenant, IServiceProvider services, IConfiguration configuration)
    : ControllerBase
{
    [HttpGet("eventos")]
    public async Task<IActionResult> GetEventos(Guid tenantId, [FromQuery] long despues, CancellationToken ct)
    {
        if (!EsConsumidor() || tenantId == Guid.Empty)
            return NotFound();
        if (despues < 0)
            return BadRequest("El cursor debe ser mayor o igual a cero.");

        tenant.TenantId = tenantId;
        var context = services.GetRequiredService<InmueblesOperacionContext>();

        try
        {
            var eventos = await (from e in context.EventoInmueble.AsNoTracking()
                                 where e.id > despues
                                 orderby e.id
                                 select new { EventoId = e.id, InmuebleId = e.inmueble_id,
                                     EstadoRevision = e.estado_revision, OcurridoEn = e.ocurrido_en })
                .Take(100).ToListAsync(ct);

            return Ok(new
            {
                TenantId = tenantId,
                SiguienteCursor = eventos.LastOrDefault()?.EventoId ?? despues,
                Eventos = eventos
            });
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.InvalidCatalogName or PostgresErrorCodes.UndefinedTable)
        {
            return NotFound();
        }
    }

    [HttpGet("{inmuebleId:guid}/elegibilidad")]
    public async Task<IActionResult> GetElegibilidad(Guid tenantId, Guid inmuebleId, CancellationToken ct)
    {
        if (!EsConsumidor() || tenantId == Guid.Empty || inmuebleId == Guid.Empty)
            return NotFound();

        tenant.TenantId = tenantId;
        var context = services.GetRequiredService<InmueblesOperacionContext>();
        try
        {
            var ficha = await (from i in context.Inmueble.AsNoTracking()
                               where i.id == inmuebleId && i.estado == "ACTIVO"
                               select new { InmuebleId = i.id, EstadoRevision = i.estado_revision }).SingleOrDefaultAsync(ct);
            return ficha is null ? NotFound() : Ok(ficha);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.InvalidCatalogName or PostgresErrorCodes.UndefinedTable)
        {
            return NotFound();
        }
    }

    [HttpGet("{inmuebleId:guid}/foto-publica")]
    [HttpHead("{inmuebleId:guid}/foto-publica")]
    public async Task<IActionResult> GetFotoPublica(Guid tenantId, Guid inmuebleId, CancellationToken ct)
    {
        if (!EsConsumidor() || tenantId == Guid.Empty || inmuebleId == Guid.Empty)
            return NotFound();

        tenant.TenantId = tenantId;
        var context = services.GetRequiredService<InmueblesOperacionContext>();
        var fotos = from i in context.Inmueble.AsNoTracking()
                    join d in context.InmuebleDocumento on i.id equals d.inmueble_id
                    join v in context.DocumentoVersion on d.documento_id equals v.documento_id
                    where i.id == inmuebleId && i.estado == "ACTIVO" && i.estado_revision == "APROBADO" &&
                          d.fecha_eliminacion == null && d.Documento.clase == FotoPublica.Clase &&
                          d.Documento.estado == "ACTIVO" && v.contenido != null
                    orderby d.fecha_creacion descending, d.documento_id, v.version descending
                    select v;

        try
        {
            if (HttpMethods.IsHead(Request.Method))
                return await fotos.AnyAsync(ct) ? Ok() : NotFound();
            var bytes = await fotos.Select(v => v.contenido).FirstOrDefaultAsync(ct);
            var tipo = bytes is null ? null : FotoPublica.Tipo(bytes);
            return tipo is null ? NotFound() : File(bytes!, tipo);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.InvalidCatalogName or PostgresErrorCodes.UndefinedTable)
        {
            return NotFound();
        }
    }

    private bool EsConsumidor()
    {
        var clave = configuration["Inmuebles:ClaveEventos"];
        return !string.IsNullOrWhiteSpace(clave) &&
            Request.Headers.TryGetValue("X-Inmuebles-Eventos", out var recibida) && recibida.Count == 1 &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(recibida.ToString()));
    }
}
