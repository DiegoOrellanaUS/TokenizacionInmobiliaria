using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CajaPagos.Dominio
{
    [Table("reserva_asiento", Schema = "caja")]
    [PrimaryKey(nameof(reserva_id), nameof(asiento_id))]
    public class ReservaAsiento
    {
        public Guid reserva_id { get; set; }

        public Guid asiento_id { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(reserva_id))]
        [JsonIgnore]
        public ReservaSaldo Reserva { get; set; } = null!;

        [ForeignKey(nameof(asiento_id))]
        [JsonIgnore]
        public Asiento Asiento { get; set; } = null!;
    }
}
