using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("vinculo_reserva", Schema = "comercio")]
    [PrimaryKey(nameof(orden_id), nameof(reserva_ref))]
    public class VinculoReserva
    {
        public Guid orden_id { get; set; }

        public Guid reserva_ref { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(orden_id))]
        [JsonIgnore]
        public Orden Orden { get; set; } = null!;
    }
}
