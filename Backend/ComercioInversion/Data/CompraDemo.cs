namespace ComercioInversion.Data;

public sealed class CompraDemo
{
    public Guid Id { get; init; }
    public Guid OfertaId { get; init; }
    public Guid CompradorId { get; init; }
    public long Unidades { get; init; }
    public decimal PrecioUnitario { get; init; }
    public required string Moneda { get; init; }
    public required string Estado { get; set; }
    public DateTimeOffset CreadaEn { get; init; }
    public DateTimeOffset ReintentarEn { get; set; }
    public bool TransferenciaIniciada { get; set; }
    public string? Hash { get; set; }
    public string? Billetera { get; set; }
    public long? Ledger { get; set; }
    public string? PagadorUsdc { get; init; }
    public string? DestinoUsdc { get; init; }
    public string? HashPago { get; set; }
    public long? LedgerPago { get; set; }
    public OfertaPublicada Oferta { get; init; } = null!;

    public static readonly Guid OfertaCasaPrado = Guid.Parse("32d2ac6f-f288-4dc9-b226-66a2379a518f");
    public static string MemoPago(Guid id) => Convert.ToBase64String(id.ToByteArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public sealed record EvidenciaPago(string Hash, string Pagador, string Destino, decimal Total, DateTimeOffset CreadaEn);
}
