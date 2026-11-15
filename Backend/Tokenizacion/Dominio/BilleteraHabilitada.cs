using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tokenizacion.Dominio
{
    [Table("billetera_habilitada", Schema = "tokenizacion")]
    [PrimaryKey(nameof(activo_id), nameof(billetera_id), nameof(desde))]
    public class BilleteraHabilitada
    {
        public Guid activo_id { get; set; }

        public Guid billetera_id { get; set; }

        public DateTimeOffset desde { get; set; }

        public DateTimeOffset? hasta { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(activo_id))]
        [JsonIgnore]
        public ActivoCadena Activo { get; set; } = null!;

        [ForeignKey(nameof(billetera_id))]
        [JsonIgnore]
        public Billetera Billetera { get; set; } = null!;
    }
}
