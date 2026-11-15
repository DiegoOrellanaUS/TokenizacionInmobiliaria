using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CajaPagos.Dominio
{
    [Table("pago_asiento", Schema = "caja")]
    [PrimaryKey(nameof(pago_id), nameof(asiento_id))]
    public class PagoAsiento
    {
        public Guid pago_id { get; set; }

        public Guid asiento_id { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(pago_id))]
        [JsonIgnore]
        public Pago Pago { get; set; } = null!;

        [ForeignKey(nameof(asiento_id))]
        [JsonIgnore]
        public Asiento Asiento { get; set; } = null!;
    }
}
