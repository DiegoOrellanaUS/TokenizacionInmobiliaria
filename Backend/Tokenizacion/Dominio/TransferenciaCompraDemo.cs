using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokenizacion.Dominio;

[Table("transferencia_compra_demo", Schema = "tokenizacion")]
public sealed class TransferenciaCompraDemo
{
    [Key]
    public Guid CompraId { get; init; }
    public Guid InmuebleId { get; init; }
    public Guid CompradorId { get; init; }
    public long Unidades { get; init; }
    public required string Estado { get; set; }
    public string? Hash { get; set; }
    public string? Sobre { get; set; }
    public string? Billetera { get; set; }
    public long? Ledger { get; set; }
}
