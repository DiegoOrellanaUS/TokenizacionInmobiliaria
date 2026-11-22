using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Tokenizacion.Servicios;

public sealed class VerificarPagoPollar(HttpClient horizon)
{
    public const string EmisorUsdc = "GBBD47IF6LWK7P7MDEVSCWR7DPUWV3NY3DTQEVFL4NAT4AQH3ZLLFLA5";
    public static readonly Guid InmuebleCasaPrado = Guid.Parse("7ec6f62c-e700-4cef-9e2c-c21a5069ac2e");

    public static bool EsCuenta(string? valor) => valor is not null && Regex.IsMatch(valor, @"\AG[A-Z2-7]{55}\z");

    public async Task<Resultado> Ejecutar(Guid compraId, Evidencia pago, CancellationToken ct)
    {
        if (pago.Hash is not { Length: 64 } || !pago.Hash.All(Uri.IsHexDigit) ||
            !EsCuenta(pago.Pagador) || !EsCuenta(pago.Destino) || pago.Pagador == pago.Destino ||
            pago.Total <= 0 || pago.CreadaEn == default)
            return new(false);

        using var respuesta = await horizon.GetAsync("transactions/" + pago.Hash, ct);
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
            return new(true);
        respuesta.EnsureSuccessStatusCode();
        using var tx = await Leer(respuesta, ct);
        var datos = tx.RootElement;
        // Horizon informa la hora de cierre del ledger con precisión de segundos.
        var desde = DateTimeOffset.FromUnixTimeSeconds(pago.CreadaEn.ToUnixTimeSeconds());
        var memo = Convert.ToBase64String(compraId.ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        if (!datos.GetProperty("successful").GetBoolean() ||
            !string.Equals(datos.GetProperty("hash").GetString(), pago.Hash, StringComparison.OrdinalIgnoreCase) ||
            datos.GetProperty("created_at").GetDateTimeOffset() < desde ||
            !Coincide(datos, "memo_type", "text") || !Coincide(datos, "memo", memo))
            return new(false);

        using var operaciones = await horizon.GetAsync("transactions/" + pago.Hash + "/operations?limit=200", ct);
        operaciones.EnsureSuccessStatusCode();
        using var lista = await Leer(operaciones, ct);
        var coincidentes = lista.RootElement.GetProperty("_embedded").GetProperty("records").EnumerateArray()
            .Count(o => Coincide(o, "type", "payment") && Coincide(o, "from", pago.Pagador) &&
                Coincide(o, "to", pago.Destino) && Coincide(o, "asset_code", "USDC") &&
                Coincide(o, "asset_issuer", EmisorUsdc) &&
                decimal.TryParse(o.GetProperty("amount").GetString(), NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var importe) && importe == pago.Total);
        var ledger = datos.GetProperty("ledger").GetInt64();
        return new(false, coincidentes == 1 && ledger > 0 ? ledger : null);
    }

    private static bool Coincide(JsonElement elemento, string campo, string valor) =>
        elemento.TryGetProperty(campo, out var propiedad) && propiedad.GetString() == valor;

    private static async Task<JsonDocument> Leer(HttpResponseMessage respuesta, CancellationToken ct)
    {
        await using var contenido = await respuesta.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(contenido, cancellationToken: ct);
    }

    public sealed record Evidencia(string Hash, string Pagador, string Destino, decimal Total, DateTimeOffset CreadaEn);
    public sealed record Resultado(bool Pendiente, long? Ledger = null);
}
