using System.Text.Json;

namespace Tokenizacion.Servicios;

public sealed class RedStellar(HttpClient client, StellarCli stellar)
{
    public async Task<(string Estado, long? Ledger)> ConfirmarOEnviarAsync(
        Guid tenantId, string hash, string sobre, CancellationToken ct)
    {
        var resultado = await ConsultarAsync(hash, ct);
        if (resultado.Estado != "NOT_FOUND")
            return resultado;

        try { return await stellar.EnviarPagoInmueble(tenantId, sobre, hash, ct); }
        catch (Exception ex) when (!ct.IsCancellationRequested && ex is StellarCliException or TimeoutException)
        {
            return await ConsultarAsync(hash, ct);
        }
    }

    private async Task<(string Estado, long? Ledger)> ConsultarAsync(string hash, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync("", new
        {
            jsonrpc = "2.0", id = 1, method = "getTransaction", @params = new { hash }
        }, ct);

        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        if (!json.RootElement.TryGetProperty("result", out var resultado) ||
            !resultado.TryGetProperty("status", out _))
            throw new StellarCliException("Stellar no devolvió el estado de la transacción.");

        return StellarCli.LeerResultado(resultado);
    }
}
