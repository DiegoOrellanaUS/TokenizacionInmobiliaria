using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CajaPagos.Dominio
{
    [Table("reserva_saldo", Schema = "caja")]
    public class ReservaSaldo
    {
        [Key]
        public Guid id { get; set; }

        public Guid cuenta_id { get; set; }

        public Guid? orden_ref { get; set; }

        public decimal? importe { get; set; }

        public string estado { get; set; } = null!;

        public DateTimeOffset? creada_en { get; set; }

        public DateTimeOffset? vence_en { get; set; }

        public string? idempotencia { get; set; }

        [ForeignKey(nameof(cuenta_id))]
        [JsonIgnore]
        public Cuenta Cuenta { get; set; } = null!;
    }
}
