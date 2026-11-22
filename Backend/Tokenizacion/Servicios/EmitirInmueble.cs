using System.Buffers.Binary;
using System.Security.Cryptography;
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

    public Task<ResultadoEmision> EjecutarAsync(Guid tenantId, Guid inmuebleId, Guid usuarioId, int cupo,
        TokenizarInmueble solicitud, CancellationToken ct) =>
        context.ConBloqueoStellarAsync(() => EmitirAsync(tenantId, inmuebleId, usuarioId, cupo, solicitud, ct), ct);

    public Task<ResultadoEmision?> ReconciliarAsync(Guid tenantId, Guid inmuebleId, CancellationToken ct) =>
        context.ConBloqueoStellarAsync<ResultadoEmision?>(async () =>
        {
            var existente = await Emisiones().SingleOrDefaultAsync(e => e.inmueble_ref == inmuebleId, ct);
            return existente is null ? null : await ContinuarAsync(tenantId, existente, ct);
        }, ct);

    private async Task<ResultadoEmision> EmitirAsync(Guid tenantId, Guid inmuebleId, Guid usuarioId, int cupo,
        TokenizarInmueble solicitud, CancellationToken ct)
    {
        // El formulario anterior enviaba INM001 por defecto.
        var solicitado = solicitud.Codigo is null or "" or "INM001" ? null : solicitud.Codigo;
        var emision = await Emisiones().SingleOrDefaultAsync(e => e.inmueble_ref == inmuebleId, ct);
        if (emision is null)
        {
            var usados = await context.EmisionInmueble.CountAsync(e => e.Instruccion.estado != Fallida, ct);
            if (usados >= cupo)
                throw new StellarRequisitoException("El tenant alcanzó el cupo de tokens propios de la suscripción.");

            var codigo = await ElegirCodigoAsync(inmuebleId, solicitado, ct);
            emision = await RegistrarAsync(tenantId, inmuebleId, usuarioId, codigo, solicitud.Unidades, ct);
        }
        else if ((solicitado is not null && emision.Instruccion.Activo.codigo != solicitado) ||
                 emision.Instruccion.cantidad_base != solicitud.Unidades * Escala)
        {
            throw new StellarRequisitoException("El inmueble ya tiene una solicitud de emisión con otros datos.");
        }

        return await ContinuarAsync(tenantId, emision, ct);
    }

    private async Task<string> ElegirCodigoAsync(Guid inmuebleId, string? solicitado, CancellationToken ct)
    {
        var activos = context.ActivoCadena.Where(a => a.red == Red);
        if (solicitado is not null)
        {
            if (await activos.AnyAsync(a => a.codigo == solicitado, ct))
                throw new StellarRequisitoException("El código ya se usa en un activo de este espacio.");
            return solicitado;
        }

        var valor = BinaryPrimitives.ReadUInt64BigEndian(SHA256.HashData(inmuebleId.ToByteArray()));
        string codigo;
        do
        {
            codigo = FormatearCodigo(valor++);
        } while (await activos.AnyAsync(a => a.codigo == codigo, ct));
        return codigo;
    }

    private static string FormatearCodigo(ulong valor)
    {
        const string alfabeto = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Span<char> codigo = stackalloc char[12];
        codigo[0] = 'I';
        for (var i = codigo.Length - 1; i > 0; i--)
        {
            codigo[i] = alfabeto[(int)(valor % 36)];
            valor /= 36;
        }
        return new string(codigo);
    }

    private async Task<ResultadoEmision> ContinuarAsync(Guid tenantId, EmisionInmueble emision, CancellationToken ct)
    {
        if (emision.Instruccion.estado != Pendiente)
            return Proyectar(emision);

        if (emision.sobre_firmado is null)
        {
            var instruccion = emision.Instruccion;
            var firmado = await stellar.FirmarPagoInmueble(tenantId, emision.inmueble_ref, instruccion.Activo.codigo,
                instruccion.Activo.emisor, instruccion.Destino!.direccion, instruccion.cantidad_base!.Value, ct);
            emision.hash = firmado.Hash;
            emision.sobre_firmado = firmado.Sobre;

            // La transacción firmada se conserva antes de cualquier envío a la red.
            await context.SaveChangesAsync(ct);
        }

        var resultado = await red.ConfirmarOEnviarAsync(tenantId, emision.hash!, emision.sobre_firmado!, ct);

        if (resultado.Ledger is > 0 && resultado.Estado is "SUCCESS" or "FAILED")
            await CerrarAsync(emision, resultado.Estado == "SUCCESS", resultado.Ledger.Value, ct);

        return Proyectar(emision);
    }

    private async Task<EmisionInmueble> RegistrarAsync(Guid tenantId, Guid inmuebleId, Guid usuarioId,
        string codigo, long unidades, CancellationToken ct)
    {
        var (emisor, distribuidora) = await stellar.PrepararCuentasInmueble(tenantId, inmuebleId, ct);
        var activo = new ActivoCadena
        {
            id = Guid.NewGuid(), red = Red, emisor = emisor, codigo = codigo,
            oferta_maxima_base = unidades * Escala, estado = Pendiente
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
