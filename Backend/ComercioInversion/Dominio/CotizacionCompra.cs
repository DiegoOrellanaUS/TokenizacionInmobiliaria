using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("cotizacion_compra", Schema = "comercio")]
    public class CotizacionCompra
    {
        [Key]
        public Guid id { get; set; }

        public Guid orden_id { get; set; }

        public long cantidad_base { get; set; }

        public decimal precio_unitario { get; set; }

        public decimal monto_a_pagar { get; set; }

        [MaxLength(12)]
        public string moneda_pago { get; set; } = null!;

        public DateTimeOffset reservada_en { get; set; }

        public DateTimeOffset vence_en { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(orden_id))]
        [JsonIgnore]
        public Orden Orden { get; set; } = null!;
    }
}
