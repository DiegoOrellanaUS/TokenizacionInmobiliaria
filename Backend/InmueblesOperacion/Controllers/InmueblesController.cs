using InmueblesOperacion.Data;
using InmueblesOperacion.Dominio;
using InmueblesOperacion.DTOs;
using InmueblesOperacion.Multitenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace InmueblesOperacion.Controllers;

[ApiController]
[Route("api/Inmuebles")]
public partial class InmueblesController(InmueblesOperacionContext context, TenantContext tenant) : ControllerBase
{
    private const string Borrador = "BORRADOR";
    private const string EnRevision = "EN_REVISION";
    private const string Observado = "CON_OBSERVACIONES";
    private const string Aprobado = "APROBADO";
    private const string DocumentoTitularidad = "TITULARIDAD";

    private IQueryable<Inmueble> Visibles() =>
        from inmueble in context.Inmueble
        where inmueble.estado != "BORRADO" && (tenant.EsTitular || inmueble.estado_revision != Borrador)
        select inmueble;

    private static IQueryable<InmuebleDetalle> Proyectar(IQueryable<Inmueble> inmuebles) =>
        from i in inmuebles
        select new InmuebleDetalle(i.id, i.nombre, i.tipo, i.ciudad, i.direccion, i.superficie_m2, i.estado_revision);

    [HttpGet("Listar", Name = "ListarInmuebles")]
    [HttpGet("ListarReal", Name = "ListarRealInmuebles")]
    public async Task<IActionResult> GetListar(CancellationToken ct) =>
        Ok(await Proyectar(Visibles().OrderBy(i => i.nombre).ThenBy(i => i.id)).ToListAsync(ct));

    [HttpGet("BandejaRevision")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> BandejaRevision(CancellationToken ct)
    {
        if (!tenant.EsRevisor)
            return StatusCode(StatusCodes.Status403Forbidden);

        var inmuebles = await (from i in Visibles().AsNoTracking()
            select new
            {
                Id = i.id, Nombre = i.nombre, EstadoRevision = i.estado_revision,
                SolicitadoEn = context.EventoInmueble
                    .Where(e => e.inmueble_id == i.id && e.estado_revision == EnRevision)
                    .Max(e => (DateTimeOffset?)e.ocurrido_en)
            }).ToListAsync(ct);
        return Ok(inmuebles);
    }

    [HttpGet("Buscar/{id:guid}", Name = "BuscarInmueble")]
    public async Task<IActionResult> GetBuscar(Guid id, CancellationToken ct)
    {
        await using var lectura = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var inmueble = await Proyectar(Visibles().Where(i => i.id == id)).SingleOrDefaultAsync(ct);
        if (inmueble is null)
            return NotFound();

        var revisiones = await (from r in context.InmuebleRevision.AsNoTracking()
                                where r.inmueble_id == id
                                orderby r.revisado_en descending, r.id
                                select new { r.id, r.decision, r.observacion, r.revisado_en }).ToListAsync(ct);

        var titulares = await (from t in context.Titularidad.AsNoTracking()
                               where t.inmueble_id == id && t.fecha_eliminacion == null
                               select new { Nombre = t.Parte.nombre, t.desde, t.cuota, t.verificada_en }).ToListAsync(ct);

        var documentos = await (from v in context.DocumentoVersion.AsNoTracking()
                                join vinculo in context.InmuebleDocumento on v.documento_id equals vinculo.documento_id
                                where vinculo.inmueble_id == id && vinculo.fecha_eliminacion == null
                                orderby v.documento_id, v.version descending
                                select new { DocumentoId = v.documento_id, v.Documento.clase, v.version, v.fecha, v.hash })
            .ToListAsync(ct);

        var envioId = inmueble.EstadoRevision == EnRevision ? await UltimoEnvioAsync(id, ct) : null;
        await lectura.CommitAsync(ct);
        return Ok(new { Inmueble = inmueble, EnvioId = envioId, Titulares = titulares, Documentos = documentos, Revisiones = revisiones });
    }

    [HttpPost("Crear", Name = "CrearInmueble")]
    public async Task<IActionResult> PostCrear(RegistroInmueble registro, CancellationToken ct)
    {
        if (!tenant.EsTitular)
            return StatusCode(StatusCodes.Status403Forbidden);
        if (!SuperficieValida(registro.SuperficieM2))
            return BadRequest("La superficie admite hasta dos decimales.");

        await using var tx = await AbrirFlujoAsync(ct);
        var inmueble = new Inmueble
        {
            id = Guid.NewGuid(), estado = "ACTIVO", creado_por_ref = tenant.UsuarioId
        };
        AplicarFicha(inmueble, registro);
        context.Inmueble.Add(inmueble);
        RegistrarEstado(inmueble, Borrador);
        await GuardarAsync(tx, ct);
        return CreatedAtRoute("BuscarInmueble", new { id = inmueble.id }, new { Id = inmueble.id, EstadoRevision = Borrador });
    }

    [HttpPut("Actualizar/{id:guid}", Name = "ActualizarInmueble")]
    public async Task<IActionResult> PutActualizar(Guid id, RegistroInmueble registro, CancellationToken ct)
    {
        if (!tenant.EsTitular)
            return StatusCode(StatusCodes.Status403Forbidden);
        if (!SuperficieValida(registro.SuperficieM2))
            return BadRequest("La superficie admite hasta dos decimales.");

        await using var tx = await AbrirFlujoAsync(ct);
        var inmueble = await Visibles().SingleOrDefaultAsync(i => i.id == id, ct);
        if (inmueble is null)
            return NotFound();
        if (!Editable(inmueble))
            return Conflict("Solo se puede editar un borrador o un inmueble observado.");

        AplicarFicha(inmueble, registro);
        await GuardarAsync(tx, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/Titulares", Name = "DeclararTitularesInmueble")]
    public async Task<IActionResult> PutTitulares(Guid id, DeclararTitulares registro, CancellationToken ct)
    {
        if (!tenant.EsTitular)
            return StatusCode(StatusCodes.Status403Forbidden);

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        if (registro.Titulares.Any(t => t is null || t.Desde == default || t.Desde > hoy))
            return BadRequest("Cada titular debe tener una fecha de inicio válida.");

        var conCuota = registro.Titulares.Count(t => t.Cuota.HasValue);
        if (conCuota > 0 && (conCuota != registro.Titulares.Length || registro.Titulares.Sum(t => t.Cuota) != 1))
            return BadRequest("Si declaras cuotas, indica la de cada titular y haz que sumen 1.");

        await using var tx = await AbrirFlujoAsync(ct);
        var inmueble = await Visibles().SingleOrDefaultAsync(i => i.id == id, ct);
        if (inmueble is null)
            return NotFound();
        if (!Editable(inmueble))
            return Conflict("La declaración solo se cambia en borrador o con observaciones.");

        var ahora = DateTimeOffset.UtcNow;
        var anteriores = await (from t in context.Titularidad
                                where t.inmueble_id == id && t.fecha_eliminacion == null
                                select t).ToListAsync(ct);

        foreach (var anterior in anteriores)
            anterior.fecha_eliminacion = ahora;

        foreach (var titular in registro.Titulares)
        {
            context.Titularidad.Add(new Titularidad
            {
                inmueble_id = id, desde = titular.Desde, cuota = titular.Cuota, fecha_creacion = ahora,
                Parte = new Parte { id = Guid.NewGuid(), nombre = titular.Nombre.Trim(), clase = "TITULAR_DECLARADO", estado = "ACTIVO" }
            });
        }

        await GuardarAsync(tx, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/EnviarRevision", Name = "EnviarInmuebleRevision")]
    public async Task<IActionResult> PostEnviarRevision(Guid id, CancellationToken ct)
    {
        if (!tenant.EsTitular)
            return StatusCode(StatusCodes.Status403Forbidden);

        await using var tx = await AbrirFlujoAsync(ct);
        var inmueble = await Visibles().SingleOrDefaultAsync(i => i.id == id, ct);
        if (inmueble is null)
            return NotFound();
        if (!Editable(inmueble))
            return Conflict("El inmueble debe estar en borrador o con observaciones.");

        var titulares = await (from t in context.Titularidad
                               where t.inmueble_id == id && t.fecha_eliminacion == null
                               select t.parte_id).AnyAsync(ct);
        var documento = await (from v in context.DocumentoVersion
                               join d in context.InmuebleDocumento on v.documento_id equals d.documento_id
                               where d.inmueble_id == id && d.fecha_eliminacion == null &&
                                     d.Documento.clase == DocumentoTitularidad && v.contenido != null
                               select v.documento_id).AnyAsync(ct);
        if (!titulares || !documento)
            return Conflict("Declara los titulares y adjunta un documento TITULARIDAD antes de enviar.");

        var envio = RegistrarEstado(inmueble, EnRevision);
        await GuardarAsync(tx, ct);
        return Ok(new { EstadoRevision = EnRevision, EnvioId = envio.id });
    }

    [HttpPost("{id:guid}/ResolverRevision", Name = "ResolverInmuebleRevision")]
    public async Task<IActionResult> PostResolverRevision(Guid id, ResolverRevision registro, CancellationToken ct)
    {
        if (!tenant.EsRevisor)
            return StatusCode(StatusCodes.Status403Forbidden);
        if (registro.Decision == Observado && string.IsNullOrWhiteSpace(registro.Observacion))
            return BadRequest("Indica qué debe corregir el titular.");

        await using var tx = await AbrirFlujoAsync(ct);
        var inmueble = await Visibles().SingleOrDefaultAsync(i => i.id == id, ct);
        if (inmueble is null)
            return NotFound();
        if (inmueble.estado_revision != EnRevision)
            return Conflict("La ficha debe estar en revisión.");
        if (await UltimoEnvioAsync(id, ct) != registro.EnvioId)
            return Conflict("El expediente fue reenviado. Consulta la versión actual antes de resolver.");

        var ahora = DateTimeOffset.UtcNow;
        var revision = new InmuebleRevision
        {
            id = Guid.NewGuid(), inmueble_id = id, decision = registro.Decision,
            observacion = registro.Observacion?.Trim(), revisor_ref = tenant.UsuarioId, revisado_en = ahora
        };
        context.InmuebleRevision.Add(revision);

        if (registro.Decision == Aprobado)
        {
            var declaraciones = await (from t in context.Titularidad
                                       where t.inmueble_id == id && t.fecha_eliminacion == null
                                       select t).ToListAsync(ct);

            foreach (var titular in declaraciones)
                titular.verificada_en = ahora;
        }

        RegistrarEstado(inmueble, registro.Decision);
        await GuardarAsync(tx, ct);
        return Ok(new { RevisionId = revision.id, EstadoRevision = inmueble.estado_revision });
    }

    private static bool Editable(Inmueble inmueble) => inmueble.estado_revision is Borrador or Observado;

    private static bool SuperficieValida(decimal superficie) => decimal.Round(superficie, 2) == superficie;

    private static void AplicarFicha(Inmueble inmueble, RegistroInmueble registro)
    {
        inmueble.nombre = registro.Nombre.Trim();
        inmueble.tipo = registro.Tipo.Trim();
        inmueble.ciudad = registro.Ciudad.Trim();
        inmueble.direccion = registro.Direccion.Trim();
        inmueble.superficie_m2 = registro.SuperficieM2;
    }

    private async Task<IDbContextTransaction> AbrirFlujoAsync(CancellationToken ct)
    {
        var tx = await context.Database.BeginTransactionAsync(ct);
        try
        {
            // Por silo, serializa cambios para mantener el orden del cursor de eventos.
            await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(58630271285980)", ct);
            return tx;
        }
        catch
        {
            await tx.DisposeAsync();
            throw;
        }
    }

    private Task<long?> UltimoEnvioAsync(Guid id, CancellationToken ct) =>
        (from e in context.EventoInmueble
         where e.inmueble_id == id && e.estado_revision == EnRevision
         select (long?)e.id).MaxAsync(ct);

    private EventoInmueble RegistrarEstado(Inmueble inmueble, string estado)
    {
        inmueble.estado_revision = estado;
        var evento = new EventoInmueble
        {
            inmueble_id = inmueble.id, estado_revision = estado, ocurrido_en = DateTimeOffset.UtcNow
        };
        context.EventoInmueble.Add(evento);
        return evento;
    }

    private async Task GuardarAsync(IDbContextTransaction transaccion, CancellationToken ct)
    {
        await context.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
    }
}
