using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("instrumento_inmueble", Schema = "comercio")]
    [PrimaryKey(nameof(instrumento_id), nameof(inmueble_ref), nameof(fecha_creacion))]
    public class InstrumentoInmueble
    {
        public Guid instrumento_id { get; set; }

        public Guid inmueble_ref { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(instrumento_id))]
        [JsonIgnore]
        public Instrumento Instrumento { get; set; } = null!;
    }
}
