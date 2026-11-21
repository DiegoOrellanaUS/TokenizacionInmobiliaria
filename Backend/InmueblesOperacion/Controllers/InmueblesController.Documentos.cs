using System.Security.Cryptography;
using InmueblesOperacion.Dominio;
using InmueblesOperacion.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InmueblesOperacion.Controllers;

public partial class InmueblesController
{
    private const int LimiteArchivo = 5 * 1024 * 1024;
    private const int LimiteSolicitud = LimiteArchivo + 64 * 1024;

    [HttpPost("{id:guid}/Documentos", Name = "AdjuntarDocumentoInmueble")]
    [RequestSizeLimit(LimiteSolicitud)]
    public Task<IActionResult> PostDocumento(Guid id, [FromForm] AdjuntarDocumento registro, CancellationToken ct) =>
        GuardarDocumentoAsync(id, null, registro.Clase, registro.Archivo, ct);

    [HttpPost("{id:guid}/Documentos/{documentoId:guid}/Versiones", Name = "ReemplazarDocumentoInmueble")]
    [RequestSizeLimit(LimiteSolicitud)]
    public Task<IActionResult> PostVersion(Guid id, Guid documentoId, [FromForm] ReemplazarDocumento registro, CancellationToken ct) =>
        GuardarDocumentoAsync(id, documentoId, null, registro.Archivo, ct);

    [HttpGet("{id:guid}/Documentos/{documentoId:guid}/Versiones/{version:int}", Name = "DescargarDocumentoInmueble")]
    public async Task<IActionResult> GetDocumento(Guid id, Guid documentoId, int version, CancellationToken ct)
    {
        var contenido = await (from inmueble in Visibles()
                               join vinculo in context.InmuebleDocumento on inmueble.id equals vinculo.inmueble_id
                               join v in context.DocumentoVersion on vinculo.documento_id equals v.documento_id
                               where inmueble.id == id && vinculo.documento_id == documentoId &&
                                     vinculo.fecha_eliminacion == null && v.version == version
                               select v.contenido).SingleOrDefaultAsync(ct);
        return contenido is null ? NotFound() : File(contenido, "application/octet-stream", $"{documentoId:N}-v{version}");
    }

    private async Task<IActionResult> GuardarDocumentoAsync(Guid id, Guid? documentoId, string? clase, IFormFile archivo,
        CancellationToken ct)
    {
        if (!tenant.EsTitular)
            return StatusCode(StatusCodes.Status403Forbidden);
        if (archivo.Length is <= 0 or > LimiteArchivo)
            return BadRequest("El tamaño del archivo debe ser mayor que cero y de hasta 5 MiB.");

        if (documentoId is null && clase is not (DocumentoTitularidad or FotoPublica.Clase or "FOTO" or "OTRO"))
            return BadRequest("La clase debe ser TITULARIDAD, FOTO_PUBLICA, FOTO u OTRO.");

        using var memoria = new MemoryStream((int)archivo.Length);
        await archivo.CopyToAsync(memoria, ct);
        var bytes = memoria.ToArray();

        await using var tx = await AbrirFlujoAsync(ct);
        var inmueble = await Visibles().SingleOrDefaultAsync(i => i.id == id, ct);
        if (inmueble is null)
            return NotFound();
        if (documentoId != null)
        {
            clase = await (from vinculo in context.InmuebleDocumento
                           where vinculo.inmueble_id == id && vinculo.documento_id == documentoId &&
                                 vinculo.fecha_eliminacion == null
                           select vinculo.Documento.clase).SingleOrDefaultAsync(ct);
            if (clase is null)
                return NotFound();
        }

        var esFoto = clase == FotoPublica.Clase;
        if (!Editable(inmueble) && !(esFoto && inmueble.estado_revision == "APROBADO"))
            return Conflict("El expediente no permite modificar ese documento en su estado actual.");
        if (esFoto && (bytes.Length > FotoPublica.MaximoBytes || FotoPublica.Tipo(bytes) is null))
            return BadRequest("La foto pública debe ser JPG, PNG o WebP y pesar como máximo 2 MiB.");

        var ahora = DateTimeOffset.UtcNow;
        var version = 1;
        var destino = documentoId ?? Guid.NewGuid();
        if (documentoId is null)
        {
            context.InmuebleDocumento.Add(new InmuebleDocumento
            {
                inmueble_id = id, fecha_creacion = ahora,
                Documento = new Documento
                {
                    id = destino, clase = clase, creado_por_ref = tenant.UsuarioId, estado = "ACTIVO"
                }
            });
        }
        else
        {
            var ultima = await (from v in context.DocumentoVersion
                                join d in context.InmuebleDocumento on v.documento_id equals d.documento_id
                                where d.inmueble_id == id && d.documento_id == destino && d.fecha_eliminacion == null
                                select (int?)v.version).MaxAsync(ct);
            if (ultima is null)
                return NotFound();
            version = ultima.Value + 1;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        context.DocumentoVersion.Add(new DocumentoVersion
        {
            documento_id = destino, version = version, fecha = ahora,
            objeto = $"postgres/{destino:N}/{version}", hash = hash, contenido = bytes
        });

        await GuardarAsync(tx, ct);
        return CreatedAtRoute("DescargarDocumentoInmueble", new { id, documentoId = destino, version },
            new { DocumentoId = destino, Version = version, Hash = hash });
    }
}
