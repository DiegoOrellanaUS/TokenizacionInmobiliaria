using System.ComponentModel.DataAnnotations;
using System.Net;
using ComercioInversion.Data;
using ComercioInversion.Multitenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ComercioInversion.Controllers;

[ApiController]
[Route("api/Ofertas")]
public sealed class OfertasController(CatalogoContext context, PrepararCatalogo preparar,
    TenantContext tenant, IHttpClientFactory clients) : ControllerBase
{
    [HttpGet("Catalogo", Name = "CatalogoInmobiliario")]
    public async Task<ActionResult<List<DatosOferta>>> Catalogo(CancellationToken ct)
    {
        await preparar.Ejecutar(context, ct);
        return await Publicas().Take(100).ToListAsync(ct);
    }

    [HttpGet("Buscar/{id:guid}", Name = "BuscarOfertaPublicada")]
    public async Task<ActionResult<DatosOferta>> Buscar(Guid id, CancellationToken ct)
    {
        await preparar.Ejecutar(context, ct);
        var oferta = await Publicas(id).SingleOrDefaultAsync(ct);
        return oferta is null ? NotFound() : Ok(oferta);
    }

    [HttpPost("{inmuebleId:guid}/Publicar", Name = "PublicarOfertaInmueble")]
    public async Task<IActionResult> Publicar(Guid inmuebleId, PublicarOferta solicitud, CancellationToken ct)
    {
        if (Request.Headers.Authorization.Count != 1)
            return Unauthorized();
        if (inmuebleId == Guid.Empty || solicitud.Precio != decimal.Round(solicitud.Precio, 2))
            return BadRequest("Indica el inmueble y un precio con hasta dos decimales.");

        DatosEmision? emision;
        DatosInmueble? inmueble;
        try
        {
            using var tokens = await Consultar("TokensOferta", $"api/Tokenizacion/Inmuebles/{inmuebleId:D}/Buscar", ct);
            if (!tokens.IsSuccessStatusCode)
                return EstadoConsulta(tokens);

            emision = await tokens.Content.ReadFromJsonAsync<DatosEmision>(ct);
            if (emision?.Estado != "CONFIRMADA")
                return Conflict("La emisión de Stellar debe estar confirmada.");
            if (solicitud.Unidades > emision.Unidades)
                return BadRequest("La oferta supera las unidades emitidas.");

            using var ficha = await Consultar("InmueblesOferta", $"api/Inmuebles/Buscar/{inmuebleId:D}", ct);
            if (!ficha.IsSuccessStatusCode)
                return EstadoConsulta(ficha);

            inmueble = (await ficha.Content.ReadFromJsonAsync<Ficha>(ct))?.Inmueble;
            if (inmueble?.EstadoRevision != "APROBADO")
                return Conflict("El expediente del inmueble debe estar aprobado.");
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException ||
            ex is OperationCanceledException && !ct.IsCancellationRequested)
        {
            return StatusCode(503, "No se pudo comprobar el expediente y la emisión.");
        }

        await preparar.Ejecutar(context, ct);
        var actual = await Existente(inmuebleId, ct);
        if (actual is not null)
            return ResolverPublicada(actual, solicitud);

        var oferta = new OfertaPublicada
        {
            Id = Guid.NewGuid(), TenantId = tenant.TenantId, InmuebleId = inmuebleId,
            Nombre = inmueble.Nombre, Ciudad = inmueble.Ciudad, Tipo = inmueble.Tipo, Codigo = emision.Codigo,
            Unidades = solicitud.Unidades, Disponibles = solicitud.Unidades, Precio = solicitud.Precio, Moneda = solicitud.Moneda,
            PublicadaEn = DateTimeOffset.UtcNow
        };
        context.Ofertas.Add(oferta);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            context.Entry(oferta).State = EntityState.Detached;
            actual = await Existente(inmuebleId, ct);
            if (actual is null)
                throw;
            return ResolverPublicada(actual, solicitud);
        }

        return CreatedAtRoute("BuscarOfertaPublicada", new { id = oferta.Id }, Datos(oferta));
    }

    private IActionResult EstadoConsulta(HttpResponseMessage respuesta) => respuesta.StatusCode switch
    {
        HttpStatusCode.Unauthorized => Unauthorized(),
        HttpStatusCode.Forbidden => StatusCode(403),
        HttpStatusCode.NotFound => NotFound("No se encontró una emisión para ese inmueble en el tenant."),
        _ => StatusCode(503, "El servicio de inmuebles o tokenización no está disponible.")
    };

    private async Task<HttpResponseMessage> Consultar(string cliente, string ruta, CancellationToken ct)
    {
        using var client = clients.CreateClient(cliente);
        using var request = new HttpRequestMessage(HttpMethod.Get, ruta);
        request.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());
        request.Headers.Add("X-Tenant", tenant.TenantId.ToString("D"));
        return await client.SendAsync(request, ct);
    }

    private IQueryable<DatosOferta> Publicas(Guid? id = null) =>
        from o in context.Ofertas.AsNoTracking()
        where id == null || o.Id == id
        orderby o.PublicadaEn descending, o.Id
        select new DatosOferta(o.Id, o.InmuebleId, o.Nombre, o.Ciudad, o.Tipo, o.Codigo, o.Unidades, o.Disponibles, o.Precio, o.Moneda, o.PublicadaEn);

    private Task<OfertaPublicada?> Existente(Guid inmuebleId, CancellationToken ct) =>
        (from o in context.Ofertas.AsNoTracking()
         where o.InmuebleId == inmuebleId && o.TenantId == tenant.TenantId
         select o).SingleOrDefaultAsync(ct);

    private IActionResult ResolverPublicada(OfertaPublicada oferta, PublicarOferta solicitud) =>
        oferta.Unidades == solicitud.Unidades && oferta.Moneda == solicitud.Moneda && oferta.Precio == solicitud.Precio
            ? Ok(Datos(oferta))
            : Conflict("El inmueble ya tiene una oferta publicada con otras condiciones.");

    private static DatosOferta Datos(OfertaPublicada o) =>
        new(o.Id, o.InmuebleId, o.Nombre, o.Ciudad, o.Tipo, o.Codigo, o.Unidades, o.Disponibles, o.Precio, o.Moneda, o.PublicadaEn);

    public sealed record PublicarOferta(
        [Range(typeof(decimal), "0.01", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] decimal Precio,
        [Required, RegularExpression("BOB|USD")] string Moneda,
        [Range(typeof(long), "1", "922337203685", ParseLimitsInInvariantCulture = true)] long Unidades);

    public sealed record DatosOferta(Guid Id, Guid InmuebleId, string Nombre, string Ciudad, string Tipo,
        string Codigo, long Unidades, long Disponibles, decimal Precio, string Moneda, DateTimeOffset PublicadaEn);

    private sealed record DatosEmision(string Estado, string Codigo, long Unidades);
    private sealed record Ficha(DatosInmueble Inmueble);
    private sealed record DatosInmueble(string Nombre, string Ciudad, string Tipo, string EstadoRevision);
}
