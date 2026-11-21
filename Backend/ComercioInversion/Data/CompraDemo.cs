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
    public string? Hash { get; set; }
    public string? Billetera { get; set; }
    public long? Ledger { get; set; }
    public OfertaPublicada Oferta { get; init; } = null!;
}
