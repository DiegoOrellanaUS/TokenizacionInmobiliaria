using System.ComponentModel.DataAnnotations;
using System.Net;
using ComercioInversion.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComercioInversion.Controllers;

[ApiController]
[Route("api/Compras")]
public sealed class ComprasController(CatalogoContext context, PrepararCatalogo preparar, IHttpClientFactory clientes,
    IWebHostEnvironment environment, IConfiguration configuration) : ControllerBase
{
    [HttpPost("Ofertas/{ofertaId:guid}", Name = "ComprarUnidadesDemo")]
    public Task<IActionResult> Comprar(Guid ofertaId, SolicitudCompra solicitud, CancellationToken ct) =>
        ConComprador(async comprador =>
        {
            if (solicitud.Referencia == Guid.Empty)
                return BadRequest("Indica una referencia de compra válida.");
            if (string.IsNullOrWhiteSpace(configuration["Comercio:ClaveLiquidacionDemo"]))
                return StatusCode(503, "La compra de demostración aún no está habilitada.");

            await using var tx = await context.Database.BeginTransactionAsync(ct);
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({solicitud.Referencia.ToString("D")}, 0))", ct);
            var existente = await context.Compras.AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == solicitud.Referencia, ct);
            if (existente != null)
            {
                if (existente.CompradorId != comprador.UsuarioId || existente.OfertaId != ofertaId ||
                    existente.Unidades != solicitud.Unidades)
                    return Conflict("La referencia ya corresponde a otra compra.");
                return Ok(await Ordenes(comprador.UsuarioId).SingleAsync(c => c.Id == existente.Id, ct));
            }

            var oferta = await context.Ofertas.AsNoTracking().SingleOrDefaultAsync(o => o.Id == ofertaId, ct);
            if (oferta is null)
                return NotFound("No se encontró la oferta.");
            if (oferta.TenantId == comprador.TenantId)
                return BadRequest("No puedes comprar unidades de tu propia oferta.");

            var reservado = await context.Ofertas.Where(o => o.Id == ofertaId && o.Disponibles >= solicitud.Unidades)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Disponibles, o => o.Disponibles - solicitud.Unidades), ct);
            if (reservado == 0)
                return Conflict("Ya no quedan suficientes unidades disponibles.");

            var ahora = DateTimeOffset.UtcNow;
            var compra = new CompraDemo
            {
                Id = solicitud.Referencia, OfertaId = ofertaId, CompradorId = comprador.UsuarioId,
                Unidades = solicitud.Unidades, PrecioUnitario = oferta.Precio, Moneda = oferta.Moneda,
                CreadaEn = ahora, ReintentarEn = ahora, Estado = "PENDIENTE",
            };
            context.Compras.Add(compra);
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            var orden = await Ordenes(comprador.UsuarioId).SingleAsync(c => c.Id == compra.Id, ct);
            return AcceptedAtRoute("BuscarCompraDemo", new { id = compra.Id }, orden);
        }, ct);

    [HttpGet("Buscar/{id:guid}", Name = "BuscarCompraDemo")]
    public Task<IActionResult> Buscar(Guid id, CancellationToken ct) => ConComprador(async comprador =>
    {
        var orden = await Ordenes(comprador.UsuarioId).SingleOrDefaultAsync(c => c.Id == id, ct);
        return orden is null ? NotFound() : Ok(orden);
    }, ct);

    [HttpGet("Portafolio", Name = "PortafolioComprador")]
    public Task<IActionResult> Portafolio(CancellationToken ct) => ConComprador(async comprador =>
        Ok(await Ordenes(comprador.UsuarioId).OrderByDescending(c => c.CreadaEn).ToListAsync(ct)), ct);

    private IQueryable<CompraDetalle> Ordenes(Guid comprador) => context.Compras.AsNoTracking()
        .Where(c => c.CompradorId == comprador)
        .Select(c => new CompraDetalle
        {
            Id = c.Id, OfertaId = c.OfertaId, Inmueble = c.Oferta.Nombre, Codigo = c.Oferta.Codigo, Unidades = c.Unidades,
            Total = c.PrecioUnitario * c.Unidades, Moneda = c.Moneda, Estado = c.Estado, CreadaEn = c.CreadaEn,
            Hash = c.Hash, Billetera = c.Billetera, Ledger = c.Ledger,
        });

    private async Task<IActionResult> ConComprador(Func<Titular, Task<IActionResult>> operacion, CancellationToken ct)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Piloto"))
            return NotFound();
        if (Request.Headers.Authorization.Count != 1)
            return Unauthorized();

        try
        {
            using var client = clientes.CreateClient("AdminCompraDemo");
            using var consulta = new HttpRequestMessage(HttpMethod.Get, "api/SesionDemo/Actual");
            consulta.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());
            using var respuesta = await client.SendAsync(consulta, ct);
            if (respuesta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return Unauthorized();
            respuesta.EnsureSuccessStatusCode();
            var titular = await respuesta.Content.ReadFromJsonAsync<Titular>(ct);
            if (titular is null || titular.UsuarioId == Guid.Empty)
                return Unauthorized();

            await preparar.Ejecutar(context, ct);
            return await operacion(titular);
        }
        catch (Exception ex) when (ex is HttpRequestException ||
            ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            return StatusCode(503, "No pudo verificarse la cuenta. Inténtalo de nuevo.");
        }
    }

    public sealed record SolicitudCompra(Guid Referencia,
        [Range(typeof(long), "1", "922337203685", ParseLimitsInInvariantCulture = true)] long Unidades);

    private sealed record Titular(Guid UsuarioId, Guid? TenantId);

    private sealed class CompraDetalle
    {
        public Guid Id { get; init; }
        public Guid OfertaId { get; init; }
        public required string Inmueble { get; init; }
        public required string Codigo { get; init; }
        public long Unidades { get; init; }
        public decimal Total { get; init; }
        public required string Moneda { get; init; }
        public required string Estado { get; init; }
        public DateTimeOffset CreadaEn { get; init; }
        public string? Hash { get; init; }
        public string? Billetera { get; init; }
        public long? Ledger { get; init; }
    }
}
