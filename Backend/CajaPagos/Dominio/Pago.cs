using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CajaPagos.Dominio
{
    [Table("pago", Schema = "caja")]
    public class Pago
    {
        [Key]
        public Guid id { get; set; }

        public Guid cuenta_id { get; set; }

        public string? proveedor { get; set; }

        public string? referencia_externa { get; set; }

        public string? clase { get; set; }

        public string estado { get; set; } = null!;

        public decimal importe { get; set; }

        public DateTimeOffset? fecha { get; set; }

        public string? idempotencia { get; set; }

        [ForeignKey(nameof(cuenta_id))]
        [JsonIgnore]
        public Cuenta Cuenta { get; set; } = null!;
    }
}
