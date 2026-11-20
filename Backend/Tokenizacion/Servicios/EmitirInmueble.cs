using Microsoft.EntityFrameworkCore;
using Tokenizacion.Data;
using Tokenizacion.Dominio;
using Tokenizacion.DTOs;

namespace Tokenizacion.Servicios;

public sealed class EmitirInmueble(TokenizacionContext context, StellarCli stellar, RedStellar red)
{
    private const string Pendiente = "PENDIENTE";
    private const string Confirmada = "CONFIRMADA";
    private const string Fallida = "FALLIDA";
    private const string Red = StellarCli.RedInmuebles;
    private const long Escala = StellarCli.UnidadesPorTokenInmueble;

    private IQueryable<EmisionInmueble> Emisiones() => context.EmisionInmueble
        .Include(e => e.Instruccion).ThenInclude(i => i.Activo)
        .Include(e => e.Instruccion).ThenInclude(i => i.Destino)
        .Include(e => e.Transaccion);

    public async Task<ResultadoEmision?> ConsultarAsync(Guid inmuebleId, CancellationToken ct)
    {
        var emision = await Emisiones().AsNoTracking().SingleOrDefaultAsync(e => e.inmueble_ref == inmuebleId, ct);
        return emision is null ? null : Proyectar(emision);
    }

    public async Task<ResultadoEmision> EjecutarAsync(Guid tenantId, Guid inmuebleId, Guid usuarioId, int cupo,
        TokenizarInmueble solicitud, CancellationToken ct)
    {
        await context.Database.OpenConnectionAsync(ct);
        try
        {
            // Mantiene el cupo y la emisión única aun cuando se ejecuten varias instancias.
            await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(67730290112711)", ct);
            try
            {
                return await EmitirAsync(tenantId, inmuebleId, usuarioId, cupo, solicitud, ct);
            }
            finally
            {
                await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(67730290112711)", CancellationToken.None);
            }
        }
        finally { await context.Database.CloseConnectionAsync(); }
    }

    private async Task<ResultadoEmision> EmitirAsync(Guid tenantId, Guid inmuebleId, Guid usuarioId, int cupo,
        TokenizarInmueble solicitud, CancellationToken ct)
    {
        var emision = await Emisiones().SingleOrDefaultAsync(e => e.inmueble_ref == inmuebleId, ct);
        if (emision is null)
        {
            var usados = await context.EmisionInmueble.CountAsync(e => e.Instruccion.estado != Fallida, ct);
            if (usados >= cupo)
                throw new StellarRequisitoException("El tenant alcanzó el cupo de tokens propios de la suscripción.");

            emision = await RegistrarAsync(tenantId, inmuebleId, usuarioId, solicitud, ct);
        }
        else if (emision.Instruccion.Activo.codigo != solicitud.Codigo ||
                 emision.Instruccion.cantidad_base != solicitud.Unidades * Escala)
        {
            throw new StellarRequisitoException("El inmueble ya tiene una solicitud de emisión con otros datos.");
        }

        if (emision.Instruccion.estado != Pendiente)
            return Proyectar(emision);

        if (emision.sobre_firmado is null)
        {
            var instruccion = emision.Instruccion;
            var firmado = await stellar.FirmarPagoInmueble(tenantId, inmuebleId, instruccion.Activo.codigo,
                instruccion.Activo.emisor, instruccion.Destino!.direccion, instruccion.cantidad_base!.Value, ct);
            emision.hash = firmado.Hash;
            emision.sobre_firmado = firmado.Sobre;

            // La transacción firmada se conserva antes de cualquier envío a la red.
            await context.SaveChangesAsync(ct);
        }

        var hash = emision.hash!;
        var resultado = await red.ConsultarAsync(hash, ct);
        if (resultado.Estado == "NOT_FOUND")
        {
            try
            {
                resultado = await stellar.EnviarPagoInmueble(tenantId, emision.sobre_firmado!, hash, ct);
            }
            catch (Exception ex) when (ex is StellarCliException or TimeoutException)
            {
                resultado = await red.ConsultarAsync(hash, ct);
            }
        }

        if (resultado.Ledger is > 0 && resultado.Estado is "SUCCESS" or "FAILED")
            await CerrarAsync(emision, resultado.Estado == "SUCCESS", resultado.Ledger.Value, ct);

        return Proyectar(emision);
    }

    private async Task<EmisionInmueble> RegistrarAsync(Guid tenantId, Guid inmuebleId, Guid usuarioId,
        TokenizarInmueble solicitud, CancellationToken ct)
    {
        var (emisor, distribuidora) = await stellar.PrepararCuentasInmueble(tenantId, inmuebleId, ct);
        var activo = new ActivoCadena
        {
            id = Guid.NewGuid(), red = Red, emisor = emisor, codigo = solicitud.Codigo,
            oferta_maxima_base = solicitud.Unidades * Escala, estado = Pendiente
        };
        var emision = new EmisionInmueble
        {
            inmueble_ref = inmuebleId, creado_por_ref = usuarioId, creada_en = DateTimeOffset.UtcNow,
            Instruccion = new InstruccionCadena
            {
                id = Guid.NewGuid(), Activo = activo, idempotencia = $"inmueble:{inmuebleId:N}", clase = "EMISION_INICIAL",
                cantidad_base = activo.oferta_maxima_base, estado = Pendiente,
                Origen = NuevaBilletera(emisor, "EMISORA", usuarioId),
                Destino = NuevaBilletera(distribuidora, "DISTRIBUIDORA", usuarioId)
            }
        };
        context.EmisionInmueble.Add(emision);
        await context.SaveChangesAsync(ct);
        return emision;
    }

    private async Task CerrarAsync(EmisionInmueble emision, bool confirmada, long ledger, CancellationToken ct)
    {
        var instruccion = emision.Instruccion;
        var transaccion = new TransaccionCadena
        {
            id = Guid.NewGuid(), red = Red, hash = emision.hash!, numero_ledger = ledger,
            estado = confirmada ? Confirmada : Fallida, confirmada_en = confirmada ? DateTimeOffset.UtcNow : null
        };
        emision.Transaccion = transaccion;
        emision.sobre_firmado = null;
        instruccion.estado = transaccion.estado;
        instruccion.Activo.estado = confirmada ? "ACTIVO" : Fallida;

        if (confirmada)
        {
            context.OperacionCadena.Add(new OperacionCadena
            {
                Transaccion = transaccion, indice = 0, Instruccion = instruccion,
                activo_id = instruccion.activo_id, origen_id = instruccion.origen_id, destino_id = instruccion.destino_id,
                clase = instruccion.clase, cantidad_base = instruccion.cantidad_base
            });
        }

        await context.SaveChangesAsync(ct);
    }

    private static Billetera NuevaBilletera(string direccion, string proposito, Guid titular) => new()
    {
        id = Guid.NewGuid(), direccion = direccion, proposito = proposito, titular_ref = titular, red = Red, estado = "ACTIVO"
    };

    private static ResultadoEmision Proyectar(EmisionInmueble emision)
    {
        var i = emision.Instruccion;
        return new ResultadoEmision(emision.inmueble_ref, i.activo_id, i.Activo.codigo, i.cantidad_base!.Value / Escala,
            Red, i.Activo.emisor, i.Destino!.direccion, i.estado, emision.hash, emision.Transaccion?.numero_ledger);
    }
}
