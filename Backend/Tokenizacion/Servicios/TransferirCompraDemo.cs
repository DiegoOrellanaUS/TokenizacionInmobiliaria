using Microsoft.EntityFrameworkCore;
using Tokenizacion.Data;
using Tokenizacion.Dominio;

namespace Tokenizacion.Servicios;

public sealed class TransferirCompraDemo(TokenizacionContext context, StellarCli stellar, RedStellar red)
{
    private const string Pendiente = "PENDIENTE";
    private const string Confirmada = "CONFIRMADA";
    private const string Fallida = "FALLIDA";

    public async Task<TransferenciaCompraDemo> Ejecutar(Guid tenantId, Guid compraId, Guid inmuebleId, Guid compradorId,
        long unidades, CancellationToken ct)
    {
        await context.Database.OpenConnectionAsync(ct);
        try
        {
            // Comparte el bloqueo con la emisión y protege la secuencia de la cuenta distribuidora.
            await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock(67730290112711)", ct);
            try { return await EjecutarConBloqueo(tenantId, compraId, inmuebleId, compradorId, unidades, ct); }
            finally
            {
                await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock(67730290112711)", CancellationToken.None);
            }
        }
        finally { await context.Database.CloseConnectionAsync(); }
    }

    private async Task<TransferenciaCompraDemo> EjecutarConBloqueo(Guid tenantId, Guid compraId, Guid inmuebleId,
        Guid compradorId, long unidades, CancellationToken ct)
    {
        var orden = await context.TransferenciasComprasDemo.SingleOrDefaultAsync(t => t.CompraId == compraId, ct);
        if (orden is not null && (orden.InmuebleId != inmuebleId || orden.CompradorId != compradorId || orden.Unidades != unidades))
            throw new StellarRequisitoException("La orden ya está vinculada a otra transferencia.");

        if (orden is not null && orden.Estado != Pendiente)
            return orden;

        if (orden is null)
        {
            orden = new TransferenciaCompraDemo
            {
                CompraId = compraId, CompradorId = compradorId, InmuebleId = inmuebleId,
                Unidades = unidades, Estado = Pendiente,
            };
            context.TransferenciasComprasDemo.Add(orden);
            await context.SaveChangesAsync(ct);
        }

        if (orden.Sobre is null)
        {
            var previo = await context.TransferenciasComprasDemo.AsNoTracking().AnyAsync(t => t.CompraId != compraId &&
                t.InmuebleId == inmuebleId && t.Estado == Pendiente && t.Hash != null, ct);
            if (previo)
                return orden;

            var emision = await context.EmisionInmueble.AsNoTracking()
                .Where(e => e.inmueble_ref == inmuebleId && e.Instruccion.estado == Confirmada)
                .Select(e => new
                {
                    e.Instruccion.Activo.codigo, e.Instruccion.Activo.emisor,
                    Distribuidora = e.Instruccion.Destino!.direccion,
                }).SingleOrDefaultAsync(ct);
            if (emision is null)
            {
                orden.Estado = Fallida;
                await context.SaveChangesAsync(ct);
                return orden;
            }

            var firmado = await stellar.PrepararTransferenciaCompra(tenantId, inmuebleId, compradorId,
                emision.codigo, emision.emisor, emision.Distribuidora, checked(unidades * StellarCli.UnidadesPorTokenInmueble), ct);
            if (firmado is null)
            {
                orden.Estado = Fallida;
                await context.SaveChangesAsync(ct);
                return orden;
            }

            orden.Hash = firmado.Value.Hash;
            orden.Sobre = firmado.Value.Sobre;
            orden.Billetera = firmado.Value.Billetera;
            await context.SaveChangesAsync(ct);
        }

        var hash = orden.Hash!;
        var resultado = await red.ConsultarAsync(hash, ct);
        if (resultado.Estado == "NOT_FOUND")
        {
            try { resultado = await stellar.EnviarPagoInmueble(tenantId, orden.Sobre!, hash, ct); }
            catch (Exception ex) when (ex is StellarCliException or TimeoutException)
            {
                resultado = await red.ConsultarAsync(hash, ct);
            }
        }

        if (resultado.Ledger is > 0 && resultado.Estado is "SUCCESS" or "FAILED")
        {
            orden.Estado = resultado.Estado == "SUCCESS" ? Confirmada : Fallida;
            orden.Ledger = resultado.Ledger;
            orden.Sobre = null;
            await context.SaveChangesAsync(ct);
        }
        return orden;
    }
}
