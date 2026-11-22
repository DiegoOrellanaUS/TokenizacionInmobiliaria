using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ComercioInversion.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComercioInversion.Controllers;

public sealed partial class ComprasController
{
    [HttpGet("Rentas/Disponible")]
    public IActionResult RentasDisponibles() => environment.IsDevelopment() ? Ok(new { Habilitadas = true }) : NotFound();

    [HttpGet("Rentas/Propias")]
    public Task<IActionResult> RentasPropias(CancellationToken ct) => ConRentas(async cuenta =>
    {
        if (cuenta.TenantId is not { } tenant) return StatusCode(403);
        var ofertas = await context.Ofertas.AsNoTracking().Where(o => o.TenantId == tenant)
            .OrderBy(o => o.Nombre).Select(o => new { o.Id, o.Nombre }).ToListAsync(ct);
        var cierres = await CierresRenta().Where(c => c.TenantId == tenant).ToListAsync(ct);
        return Ok(new { Ofertas = ofertas, Cierres = cierres.Select(DetalleRenta) });
    }, ct);

    [HttpGet("Rentas/Revision")]
    public Task<IActionResult> RentasRevision(CancellationToken ct) => ConRentas(async _ =>
        Ok((await CierresRenta().ToListAsync(ct)).Select(DetalleRenta)), ct, revisor: true);

    [HttpGet("Rentas/Portafolio")]
    public Task<IActionResult> RentasPortafolio(CancellationToken ct) => ConRentas(async cuenta =>
    {
        var pagos = await context.PagosRenta.AsNoTracking()
            .Include(p => p.Cierre).ThenInclude(c => c.Oferta)
            .Where(p => p.CompradorId == cuenta.UsuarioId && p.Cierre.AprobadoEn != null)
            .OrderByDescending(p => p.Cierre.Periodo).ToListAsync(ct);
        return Ok(pagos.Select(DetallePago));
    }, ct);

    [HttpPost("Rentas/Ofertas/{ofertaId:guid}")]
    public Task<IActionResult> CrearRenta(Guid ofertaId, SolicitudRenta solicitud, CancellationToken ct) =>
        ConRentas(async cuenta =>
        {
            if (cuenta.TenantId is not { } tenant) return StatusCode(403);
            var hoy = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-4)).DateTime);
            if (!DateOnly.TryParseExact(solicitud.Periodo + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var periodo) || periodo >= new DateOnly(hoy.Year, hoy.Month, 1))
                return BadRequest("Elige un mes que ya terminó en Bolivia.");
            if (!ContabilidadRenta.Valida(solicitud, hoy))
                return BadRequest("Revisa los importes, créditos, fechas y referencias de los comprobantes.");

            var oferta = await context.Ofertas.SingleOrDefaultAsync(o => o.Id == ofertaId && o.TenantId == tenant, ct);
            if (oferta is null) return NotFound();
            var emision = await SolicitarRenta<EmisionRenta>(tenant, $"emision/{oferta.InmuebleId:D}", null, ct);
            if (emision is not { Unidades: > 0 } || emision.Codigo != oferta.Codigo)
                return Conflict("No se pudo comprobar la emisión confirmada para esta oferta.");

            await using var tx = await context.Database.BeginTransactionAsync(ct);
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({ofertaId.ToString("D") + solicitud.Periodo}, 27))", ct);
            if (await context.CierresRenta.AnyAsync(c => c.OfertaId == ofertaId && c.Periodo == solicitud.Periodo, ct))
                return Conflict("Ya se registró un cierre para ese mes e inmueble.");

            var corte = DateTimeOffset.UtcNow;
            var titulares = await context.Compras
                .Where(c => c.OfertaId == ofertaId && c.Estado == "CONFIRMADA" && c.Ledger > 0 &&
                    c.CreadaEn <= corte)
                .GroupBy(c => new { c.CompradorId, c.Billetera })
                .Select(g => new { g.Key.CompradorId, g.Key.Billetera, Unidades = g.Sum(c => c.Unidades) })
                .ToListAsync(ct);
            if (titulares.Select(t => t.CompradorId).Distinct().Count() != titulares.Count ||
                titulares.Any(t => t.Billetera is not { } wallet || !Regex.IsMatch(wallet, @"\AG[A-Z2-7]{55}\z")) ||
                titulares.Sum(t => t.Unidades) > emision.Unidades)
                return Conflict("Las entregas registradas no coinciden con los titulares o el total de la emisión.");

            var contabilidad = ContabilidadRenta.Calcular(solicitud, emision.Unidades);
            var cierre = new CierreRenta
            {
                Id = Guid.NewGuid(), OfertaId = ofertaId, TenantId = tenant, Oferta = oferta,
                Periodo = solicitud.Periodo, Corte = corte, AutorId = cuenta.UsuarioId,
                Datos = JsonSerializer.Serialize(contabilidad, JsonSerializerOptions.Web),
            };
            foreach (var titular in titulares)
            {
                var centavos = (long)decimal.Floor(contabilidad.Distribuible * 100 * titular.Unidades / emision.Unidades);
                var stroops = (long)decimal.Floor(centavos * 100_000m / solicitud.BobPorXlm);
                cierre.Pagos.Add(new PagoRenta
                {
                    Id = Guid.NewGuid(), CierreId = cierre.Id, CompradorId = titular.CompradorId,
                    Billetera = titular.Billetera!, Unidades = titular.Unidades, Centavos = centavos, Stroops = stroops,
                });
            }
            context.CierresRenta.Add(cierre);
            try { await context.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
            catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException { SqlState: "23505" })
            {
                return Conflict("El cierre ya fue registrado. Actualiza la lista.");
            }
            return Ok(DetalleRenta(cierre));
        }, ct);

    [HttpPost("Rentas/{id:guid}/Aprobar")]
    public Task<IActionResult> AprobarRenta(Guid id, CancellationToken ct) => ConRentas(async cuenta =>
    {
        var ahora = DateTimeOffset.UtcNow;
        var filas = await context.CierresRenta.Where(c => c.Id == id && c.AprobadoEn == null &&
            c.AutorId != cuenta.UsuarioId).ExecuteUpdateAsync(p => p
                .SetProperty(c => c.AprobadoEn, ahora).SetProperty(c => c.RevisorId, cuenta.UsuarioId), ct);
        if (filas > 0 || await context.CierresRenta.AnyAsync(c => c.Id == id && c.AprobadoEn != null, ct))
            return NoContent();
        return Conflict("El cierre no está disponible para aprobación por esta cuenta.");
    }, ct, revisor: true);

    [HttpPost("Rentas/{id:guid}/Pagar")]
    public Task<IActionResult> PagarRenta(Guid id, CancellationToken ct) => ConRentas(async _ =>
    {
        await context.Database.OpenConnectionAsync(ct);
        try
        {
            var clave = id.ToString("D");
            await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_lock(hashtextextended({clave}, 28))", ct);
            try { return await EnviarSiguiente(id, ct); }
            finally
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_unlock(hashtextextended({clave}, 28))", CancellationToken.None);
            }
        }
        finally { await context.Database.CloseConnectionAsync(); }
    }, ct, revisor: true);

    private async Task<IActionResult> EnviarSiguiente(Guid id, CancellationToken ct)
    {
        var cierre = await context.CierresRenta.FindAsync([id], ct);
        if (cierre is null) return NotFound();
        if (cierre.AprobadoEn is null) return Conflict("El revisor debe aprobar el cierre antes de enviar pagos.");
        var pendientes = context.PagosRenta.Where(p => p.CierreId == id && p.Stroops > 0 &&
            (p.Ledger == null || p.Fallido));
        var pago = await pendientes.OrderByDescending(p => p.Sobre != null).ThenBy(p => p.Id).FirstOrDefaultAsync(ct);
        if (pago is null) return Ok(new { Restantes = 0 });
        if (pago.Sobre is null)
        {
            var firmado = await SolicitarRenta<FirmadoRenta>(cierre.TenantId, $"{id:D}/firmar",
                new { pago.Stroops, pago.Billetera }, ct);
            if (firmado is null) return StatusCode(503, "No se pudo firmar el pago en Testnet.");
            pago.Hash = firmado.Hash;
            pago.Sobre = firmado.Sobre;
            pago.Ledger = null;
            pago.Fallido = false;
            await context.SaveChangesAsync(ct);
        }
        var resultado = await SolicitarRenta<ResultadoRenta>(cierre.TenantId, "enviar",
            new { pago.Hash, pago.Sobre }, ct);
        if (resultado?.Ledger is not > 0 || resultado.Estado is not ("SUCCESS" or "FAILED"))
            return StatusCode(503, "Stellar aún no confirmó el pago. Reintenta el mismo envío.");
        pago.Ledger = resultado.Ledger;
        pago.Fallido = resultado.Estado == "FAILED";
        pago.Sobre = null;
        await context.SaveChangesAsync(ct);
        return pago.Fallido ? Conflict("Stellar rechazó el pago de prueba. Puedes reintentarlo.") :
            Ok(new { Restantes = await pendientes.CountAsync(ct) });
    }

    private Task<IActionResult> ConRentas(Func<Titular, Task<IActionResult>> operacion, CancellationToken ct, bool revisor = false) =>
        environment.IsDevelopment() ? ConCuenta(operacion, ct, revisor) : Task.FromResult<IActionResult>(NotFound());

    private IQueryable<CierreRenta> CierresRenta() => context.CierresRenta.AsNoTracking()
        .Include(c => c.Oferta).Include(c => c.Pagos).OrderByDescending(c => c.Periodo).ThenByDescending(c => c.Corte);

    private async Task<T?> SolicitarRenta<T>(Guid tenant, string ruta, object? cuerpo, CancellationToken ct)
    {
        var clave = configuration["Comercio:ClaveLiquidacionDemo"];
        if (string.IsNullOrWhiteSpace(clave)) return default;
        using var client = clientes.CreateClient("LiquidacionDemo");
        using var req = new HttpRequestMessage(cuerpo is null ? HttpMethod.Get : HttpMethod.Post,
            $"interno/rentas-demo/{tenant:D}/{ruta}");
        req.Headers.Add("X-Liquidacion-Demo", clave);
        if (cuerpo is not null) req.Content = JsonContent.Create(cuerpo);
        using var resp = await client.SendAsync(req, ct);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadFromJsonAsync<T>(ct) : default;
    }

    private static object DetalleRenta(CierreRenta cierre)
    {
        var contabilidad = JsonSerializer.Deserialize<ContabilidadRenta>(cierre.Datos, JsonSerializerOptions.Web)!;
        var asignado = cierre.Pagos.Sum(p => p.Centavos) / 100m;
        return new
        {
            cierre.Id, Inmueble = cierre.Oferta.Nombre, cierre.Periodo, cierre.Corte, cierre.AprobadoEn,
            Contabilidad = contabilidad, Asignado = asignado,
            SinAsignar = contabilidad.Distribuible - asignado,
            Pendientes = cierre.Pagos.Count(p => p.Stroops > 0 && (p.Ledger == null || p.Fallido)),
            Pagados = cierre.Pagos.Count(p => p.Ledger.HasValue && !p.Fallido),
        };
    }

    private static object DetallePago(PagoRenta pago) => new
    {
        Referencia = pago.Id, pago.Cierre.Periodo, pago.Cierre.Corte, Inmueble = pago.Cierre.Oferta.Nombre,
        pago.Unidades, Bob = pago.Centavos / 100m, Xlm = pago.Stroops / 10_000_000m,
        Estado = pago.Stroops == 0 ? "SIN_PAGO" : pago.Fallido ? "FALLIDO" :
            pago.Ledger.HasValue ? "PAGADO" : "PENDIENTE",
        Hash = pago.Fallido || pago.Ledger == null ? null : pago.Hash,
    };

    private sealed record EmisionRenta(long Unidades, string Codigo);
    private sealed record FirmadoRenta(string Hash, string Sobre);
    private sealed record ResultadoRenta(string Estado, long? Ledger);
}
