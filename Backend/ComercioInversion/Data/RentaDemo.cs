namespace ComercioInversion.Data;

public sealed class CierreRenta
{
    public Guid Id { get; init; }
    public Guid OfertaId { get; init; }
    public Guid TenantId { get; init; }
    public string Periodo { get; init; } = "";
    public DateTimeOffset Corte { get; init; }
    public Guid AutorId { get; init; }
    public Guid? RevisorId { get; set; }
    public DateTimeOffset? AprobadoEn { get; set; }
    public string Datos { get; init; } = "";
    public OfertaPublicada Oferta { get; init; } = null!;
    public List<PagoRenta> Pagos { get; init; } = [];
}

public sealed class PagoRenta
{
    public Guid Id { get; init; }
    public Guid CierreId { get; init; }
    public Guid CompradorId { get; init; }
    public long Unidades { get; init; }
    public long Centavos { get; init; }
    public long Stroops { get; init; }
    public string Billetera { get; init; } = "";
    public string? Sobre { get; set; }
    public string? Hash { get; set; }
    public long? Ledger { get; set; }
    public bool Fallido { get; set; }
    public CierreRenta Cierre { get; init; } = null!;
}
