using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CajaPagos.Dominio
{
    [Table("conversion_pago", Schema = "caja")]
    public class ConversionPago
    {
        [Key]
        public Guid id { get; set; }

        public Guid? pago_id { get; set; }

        public string? proveedor { get; set; }

        public string? orden_proveedor { get; set; }

        [MaxLength(12)]
        public string? moneda_origen { get; set; }

        public decimal? monto_origen { get; set; }

        [MaxLength(12)]
        public string? moneda_destino { get; set; }

        public decimal? monto_destino { get; set; }

        public decimal? tipo_cambio_ejecutado { get; set; }

        public DateTimeOffset? ejecutada_en { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(pago_id))]
        [JsonIgnore]
        public Pago? Pago { get; set; }
    }
}
