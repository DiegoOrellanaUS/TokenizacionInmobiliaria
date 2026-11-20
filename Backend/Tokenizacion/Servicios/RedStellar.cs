using System.Text.Json;

namespace Tokenizacion.Servicios;

public sealed class RedStellar(HttpClient client)
{
    public async Task<(string Estado, long? Ledger)> ConsultarAsync(string hash, CancellationToken ct)
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
