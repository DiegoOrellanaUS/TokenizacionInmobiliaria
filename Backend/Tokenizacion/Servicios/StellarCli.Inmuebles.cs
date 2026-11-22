using System.Globalization;
using System.Text.Json;

namespace Tokenizacion.Servicios;

public sealed partial class StellarCli
{
    public const long UnidadesPorTokenInmueble = UnidadesPorToken;
    public const string RedInmuebles = Red;

    public async Task<(string Emisor, string Distribuidora)> PrepararCuentasInmueble(
        Guid tenantId, Guid inmuebleId, CancellationToken ct)
    {
        var emisor = await PrepararCuentaAsync(tenantId, AliasEmisor(inmuebleId), ct);
        var distribuidora = await PrepararCuentaAsync(tenantId, AliasDistribuidora(inmuebleId), ct);
        return (emisor, distribuidora);
    }

    public async Task<(string Hash, string Sobre)> FirmarPagoInmueble(
        Guid tenantId, Guid inmuebleId, string codigo, string emisor, string distribuidora, long cantidad, CancellationToken ct)
    {
        var activo = $"{codigo}:{emisor}";
        var cantidadCli = cantidad.ToString(CultureInfo.InvariantCulture);
        var linea = await ConsultarLineaAsync(tenantId, activo, distribuidora, ct);
        if (linea is null)
            await EnviarAsync(tenantId, AliasDistribuidora(inmuebleId), ct, "change-trust",
                "--line", activo, "--limit", cantidadCli);
        else if (linea.Value.Saldo != 0 || linea.Value.Limite != cantidad)
            throw new StellarRequisitoException("La línea de confianza del inmueble no coincide con la emisión pendiente.");

        return await FirmarPagoAsync(tenantId, AliasEmisor(inmuebleId), activo, distribuidora, cantidadCli, ct);
    }

    public async Task<(string Hash, string Sobre, string Billetera)?> PrepararTransferenciaCompra(
        Guid tenantId, Guid inmuebleId, Guid compradorId, string codigo, string emisor, string distribuidora,
        long cantidad, CancellationToken ct)
    {
        var activo = $"{codigo}:{emisor}";
        var firmante = AliasDistribuidora(inmuebleId);
        if (await ObtenerDireccionAsync(tenantId, firmante, ct) != distribuidora)
            throw new StellarRequisitoException("La cuenta distribuidora no coincide con la del inmueble.");
        var origen = await ConsultarLineaAsync(tenantId, activo, distribuidora, ct);
        if (origen is null || origen.Value.Saldo < cantidad)
            return null;

        var aliasComprador = $"comprador-{compradorId:N}";
        var comprador = await PrepararCuentaAsync(tenantId, aliasComprador, ct);
        var destino = await ConsultarLineaAsync(tenantId, activo, comprador, ct);
        if (destino is null)
            await EnviarAsync(tenantId, aliasComprador, ct, "change-trust",
                "--line", activo, "--limit", long.MaxValue.ToString(CultureInfo.InvariantCulture));
        else if (destino.Value.Limite - destino.Value.Saldo < cantidad)
            return null;

        var firmado = await FirmarPagoAsync(tenantId, firmante, activo, comprador,
            cantidad.ToString(CultureInfo.InvariantCulture), ct);
        return (firmado.Hash, firmado.Sobre, comprador);
    }

    public async Task<(string Hash, string Sobre)> FirmarRentaDemo(Guid tenantId, Guid cierreId,
        string destino, long stroops, CancellationToken ct)
    {
        var alias = $"renta-{cierreId:N}";
        await PrepararCuentaAsync(tenantId, alias, ct);
        return await FirmarPagoAsync(tenantId, alias, "native", destino, stroops.ToString(CultureInfo.InvariantCulture), ct);
    }

    private async Task<(string Hash, string Sobre)> FirmarPagoAsync(
        Guid tenantId, string firmante, string activo, string distribuidora, string cantidadCli, CancellationToken ct)
    {
        var sinFirma = await EjecutarAsync(tenantId, ct, null, "tx", "new", "payment",
            "--destination", distribuidora, "--asset", activo, "--amount", cantidadCli,
            "--source", firmante, "--network", Red, "--build-only");

        var sobre = await EjecutarAsync(tenantId, ct, sinFirma, "tx", "sign", "--sign-with-key", firmante, "--network", Red);
        var hash = await EjecutarAsync(tenantId, ct, sobre, "tx", "hash", "--network", Red);
        if (hash.Length != 64 || !hash.All(Uri.IsHexDigit))
            throw new StellarCliException("Stellar CLI no devolvió un hash válido.");

        return (hash.ToLowerInvariant(), sobre);
    }

    public async Task<(string Estado, long? Ledger)> EnviarPagoInmueble(Guid tenantId, string sobre, string hash, CancellationToken ct)
    {
        var respuesta = await EjecutarJsonAsync(tenantId, ct, sobre, "tx", "send", "--network", Red);
        if (!respuesta.TryGetProperty("tx_hash", out var hashEnviado) ||
            !string.Equals(hashEnviado.GetString(), hash, StringComparison.OrdinalIgnoreCase))
            throw new StellarCliException("El hash de la respuesta difiere de la emisión pendiente.");

        return LeerResultado(respuesta);
    }

    public static (string Estado, long? Ledger) LeerResultado(JsonElement respuesta)
    {
        var estado = respuesta.GetProperty("status").GetString() ?? "NOT_FOUND";
        var ledger = respuesta.TryGetProperty("ledger", out var valor) ? valor.GetInt64() : (long?)null;
        return (estado, ledger);
    }

    private static string AliasEmisor(Guid inmuebleId) => $"inmueble-{inmuebleId:N}-emisor";
    private static string AliasDistribuidora(Guid inmuebleId) => $"inmueble-{inmuebleId:N}-distrib";
}
