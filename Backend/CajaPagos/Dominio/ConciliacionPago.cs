using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CajaPagos.Dominio
{
    [Table("conciliacion_pago", Schema = "caja")]
    [PrimaryKey(nameof(conciliacion_id), nameof(pago_id))]
    public class ConciliacionPago
    {
        public Guid conciliacion_id { get; set; }

        public Guid pago_id { get; set; }

        public string? resultado { get; set; }

        public decimal? diferencia { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(conciliacion_id))]
        [JsonIgnore]
        public Conciliacion Conciliacion { get; set; } = null!;

        [ForeignKey(nameof(pago_id))]
        [JsonIgnore]
        public Pago Pago { get; set; } = null!;
    }
}
