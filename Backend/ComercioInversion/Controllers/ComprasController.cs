using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
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

            ConfiguracionPollar? pollar = null;
            if (oferta.Id == CompraDemo.OfertaCasaPrado)
            {
                if (solicitud.PagadorUsdc is null || !Regex.IsMatch(solicitud.PagadorUsdc, @"\AG[A-Z2-7]{55}\z"))
                    return BadRequest("Conecta una billetera Stellar Testnet con Pollar para pagar esta oferta.");

                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock(hashtextextended({comprador.UsuarioId.ToString("D")}, 1))", ct);
                var abierta = await OrdenActiva(comprador.UsuarioId, ct);
                if (abierta is not null)
                    return Ok(abierta);

                pollar = await LeerPollar(oferta.TenantId, ct);
                if (pollar is null)
                    return StatusCode(503, "El pago Pollar en Testnet aún no está configurado.");
                if (pollar.Destino == solicitud.PagadorUsdc)
                    return BadRequest("Usa una billetera distinta a la cuenta que recibe USDC.");
            }

            var reservado = await context.Ofertas.Where(o => o.Id == ofertaId && o.Disponibles >= solicitud.Unidades)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Disponibles, o => o.Disponibles - solicitud.Unidades), ct);
            if (reservado == 0)
                return Conflict("Ya no quedan suficientes unidades disponibles.");

            var ahora = DateTimeOffset.UtcNow;
            var compra = new CompraDemo
            {
                Id = solicitud.Referencia, OfertaId = ofertaId, CompradorId = comprador.UsuarioId,
                Unidades = solicitud.Unidades, PrecioUnitario = pollar?.PrecioUnitario ?? oferta.Precio,
                Moneda = pollar is null ? oferta.Moneda : "USD", CreadaEn = ahora, ReintentarEn = ahora,
                Estado = pollar is null ? "PENDIENTE" : "ESPERANDO_PAGO",
                PagadorUsdc = pollar is null ? null : solicitud.PagadorUsdc, DestinoUsdc = pollar?.Destino,
            };
            context.Compras.Add(compra);
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            var orden = await Ordenes(comprador.UsuarioId).SingleAsync(c => c.Id == compra.Id, ct);
            return AcceptedAtRoute("BuscarCompraDemo", new { id = compra.Id }, orden);
        }, ct);

    [HttpGet("Ofertas/{ofertaId:guid}/Pollar")]
    public Task<IActionResult> Pollar(Guid ofertaId, CancellationToken ct) => ConComprador(async comprador =>
    {
        if (ofertaId != CompraDemo.OfertaCasaPrado) return NotFound();
        var oferta = await context.Ofertas.AsNoTracking().SingleOrDefaultAsync(o => o.Id == ofertaId, ct);
        if (oferta is null) return NotFound();
        if (oferta.TenantId == comprador.TenantId) return BadRequest("No puedes comprar unidades de tu propia oferta.");
        var orden = await OrdenActiva(comprador.UsuarioId, ct);
        var configuracion = await LeerPollar(oferta.TenantId, ct);
        if (configuracion is null && orden is null)
            return StatusCode(503, "El pago Pollar en Testnet aún no está configurado.");
        return Ok(new { Configuracion = configuracion, Orden = orden });
    }, ct);

    [HttpPost("{id:guid}/IniciarPago")]
    public Task<IActionResult> IniciarPago(Guid id, CancellationToken ct) => ConComprador(async comprador =>
    {
        var actualizadas = await context.Compras.Where(c => c.Id == id && c.CompradorId == comprador.UsuarioId &&
                c.Estado == "ESPERANDO_PAGO" && !c.TransferenciaIniciada)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.TransferenciaIniciada, true), ct);
        if (actualizadas == 0)
            return Conflict("El pago ya se inició o la reserva se canceló. Consulta el historial antes de reenviar.");
        return Ok(await Ordenes(comprador.UsuarioId).SingleAsync(c => c.Id == id, ct));
    }, ct);

    [HttpPost("{id:guid}/Pago")]
    public Task<IActionResult> Pago(Guid id, SolicitudPago solicitud, CancellationToken ct) => ConComprador(async comprador =>
    {
        if (!Regex.IsMatch(solicitud.Hash, @"\A[0-9a-fA-F]{64}\z"))
            return BadRequest("Indica un hash de transacción Stellar válido.");
        var hash = solicitud.Hash.ToLowerInvariant();
        var compra = await context.Compras.AsNoTracking().Include(c => c.Oferta)
            .SingleOrDefaultAsync(c => c.Id == id && c.CompradorId == comprador.UsuarioId, ct);
        if (compra is null) return NotFound();
        if (compra.HashPago == hash)
            return Ok(await Ordenes(comprador.UsuarioId).SingleAsync(c => c.Id == id, ct));
        if (compra.Estado != "ESPERANDO_PAGO" || compra.PagadorUsdc is null ||
            compra.DestinoUsdc is null || !compra.TransferenciaIniciada)
            return Conflict("La orden no tiene un pago Pollar iniciado.");

        using var consulta = PeticionPollar(HttpMethod.Post, compra.Oferta.TenantId, $"{id:D}/pago");
        consulta.Content = JsonContent.Create(new CompraDemo.EvidenciaPago(hash, compra.PagadorUsdc,
            compra.DestinoUsdc, compra.PrecioUnitario * compra.Unidades, compra.CreadaEn));
        using var client = clientes.CreateClient("LiquidacionDemo");
        using var respuesta = await client.SendAsync(consulta, ct);
        if (!respuesta.IsSuccessStatusCode)
            return StatusCode(503, "Tokenización no pudo verificar el pago. Consulta de nuevo con el mismo hash.");
        var verificacion = await respuesta.Content.ReadFromJsonAsync<PagoVerificado>(ct);
        if (verificacion?.Pendiente == true)
            return StatusCode(425, "Horizon aún no muestra el pago. Espera unos segundos y consulta con el mismo hash.");
        if (verificacion?.Ledger is not > 0)
            return UnprocessableEntity("El pago no coincide con la orden. Revisa la red, el activo, el importe, la billetera y la referencia.");

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({hash}, 2))", ct);
        if (await context.Compras.AnyAsync(c => c.HashPago == hash && c.Id != id, ct))
            return Conflict("El pago ya está asociado a otra orden.");
        var ahora = DateTimeOffset.UtcNow;
        await context.Compras.Where(c => c.Id == id && c.Estado == "ESPERANDO_PAGO" && c.TransferenciaIniciada)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, "PENDIENTE").SetProperty(c => c.HashPago, hash)
                .SetProperty(c => c.LedgerPago, verificacion.Ledger).SetProperty(c => c.ReintentarEn, ahora), ct);
        var detalle = await Ordenes(comprador.UsuarioId).SingleAsync(c => c.Id == id, ct);
        if (detalle.HashPago != hash) return Conflict("La orden ya tiene otro pago registrado.");
        await tx.CommitAsync(ct);
        return Ok(detalle);
    }, ct);

    [HttpGet("Buscar/{id:guid}", Name = "BuscarCompraDemo")]
    public Task<IActionResult> Buscar(Guid id, CancellationToken ct) => ConComprador(async comprador =>
    {
        var orden = await Ordenes(comprador.UsuarioId).SingleOrDefaultAsync(c => c.Id == id, ct);
        return orden is null ? NotFound() : Ok(orden);
    }, ct);

    [HttpPost("{id:guid}/Cancelar")]
    public Task<IActionResult> Cancelar(Guid id, CancellationToken ct) => ConComprador(async comprador =>
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);
        var compra = await context.Compras.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id && c.CompradorId == comprador.UsuarioId, ct);
        if (compra is null) return NotFound();

        var filas = await context.Compras
            .Where(c => c.Id == id && (c.Estado == "PENDIENTE" || c.Estado == "ESPERANDO_PAGO") && !c.TransferenciaIniciada)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, "CANCELADA"), ct);
        if (filas == 1)
        {
            await context.Ofertas.Where(o => o.Id == compra.OfertaId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Disponibles, o => o.Disponibles + compra.Unidades), ct);
        }

        var detalle = await Ordenes(comprador.UsuarioId).SingleAsync(c => c.Id == id, ct);
        if (detalle.Estado != "CANCELADA")
            return Conflict("La transferencia ya se inició o la compra ya se resolvió.");

        await tx.CommitAsync(ct);
        return Ok(detalle);
    }, ct);

    [HttpGet("Portafolio", Name = "PortafolioComprador")]
    public Task<IActionResult> Portafolio(CancellationToken ct) => ConComprador(async comprador =>
        Ok(await Ordenes(comprador.UsuarioId).OrderByDescending(c => c.CreadaEn).ToListAsync(ct)), ct);

    private Task<CompraDetalle?> OrdenActiva(Guid comprador, CancellationToken ct) =>
        Ordenes(comprador).Where(c => c.OfertaId == CompraDemo.OfertaCasaPrado &&
            (c.Estado == "ESPERANDO_PAGO" || c.Estado == "PENDIENTE")).OrderByDescending(c => c.CreadaEn).FirstOrDefaultAsync(ct);

    private HttpRequestMessage PeticionPollar(HttpMethod metodo, Guid tenant, string ruta)
    {
        var request = new HttpRequestMessage(metodo, $"interno/tenants/{tenant:D}/compras-demo/{ruta}");
        request.Headers.Add("X-Liquidacion-Demo", configuration["Comercio:ClaveLiquidacionDemo"]);
        return request;
    }

    private async Task<ConfiguracionPollar?> LeerPollar(Guid tenant, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(configuration["Comercio:ClaveLiquidacionDemo"]))
            return null;
        using var client = clientes.CreateClient("LiquidacionDemo");
        using var request = PeticionPollar(HttpMethod.Get, tenant, "pollar");
        using var respuesta = await client.SendAsync(request, ct);
        return respuesta.IsSuccessStatusCode ? await respuesta.Content.ReadFromJsonAsync<ConfiguracionPollar>(ct) : null;
    }

    private IQueryable<CompraDetalle> Ordenes(Guid comprador) => context.Compras.AsNoTracking()
        .Where(c => c.CompradorId == comprador)
        .Select(c => new CompraDetalle
        {
            Id = c.Id, OfertaId = c.OfertaId, Inmueble = c.Oferta.Nombre, Codigo = c.Oferta.Codigo, Unidades = c.Unidades,
            Total = c.PrecioUnitario * c.Unidades, Moneda = c.Moneda, Estado = c.Estado, CreadaEn = c.CreadaEn,
            Hash = c.Hash, Billetera = c.Billetera, Ledger = c.Ledger,
            HashPago = c.HashPago, LedgerPago = c.LedgerPago, PagadorUsdc = c.PagadorUsdc, DestinoUsdc = c.DestinoUsdc,
            PagoIniciado = c.PagadorUsdc != null && c.TransferenciaIniciada,
            PuedeCancelar = (c.Estado == "PENDIENTE" || c.Estado == "ESPERANDO_PAGO") && !c.TransferenciaIniciada,
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
            return StatusCode(503, "No se pudo completar la consulta. Inténtalo de nuevo.");
        }
    }

    public sealed record SolicitudCompra(Guid Referencia,
        [Range(typeof(long), "1", "922337203685", ParseLimitsInInvariantCulture = true)] long Unidades,
        [MaxLength(56)] string? PagadorUsdc);

    public sealed record SolicitudPago([Required, StringLength(64, MinimumLength = 64)] string Hash);
    private sealed record ConfiguracionPollar(string ClavePublica, string Destino, string Emisor, decimal PrecioUnitario);
    private sealed record PagoVerificado(bool Pendiente, long? Ledger);

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
        public string? HashPago { get; init; }
        public long? LedgerPago { get; init; }
        public string? PagadorUsdc { get; init; }
        public string? DestinoUsdc { get; init; }
        public string? MemoPago => PagadorUsdc is null ? null : CompraDemo.MemoPago(Id);
        public string? TotalUsdc => PagadorUsdc is null ? null : Total.ToString("0.00", CultureInfo.InvariantCulture);
        public bool PagoIniciado { get; init; }
        public bool PuedeCancelar { get; init; }
    }
}
