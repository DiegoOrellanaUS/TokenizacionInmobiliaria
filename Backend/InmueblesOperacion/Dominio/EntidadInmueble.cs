using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("entidad_inmueble", Schema = "operacion")]
    [PrimaryKey(nameof(entidad_id), nameof(inmueble_id), nameof(fecha_creacion))]
    public class EntidadInmueble
    {
        public Guid entidad_id { get; set; }

        public Guid inmueble_id { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(entidad_id))]
        [JsonIgnore]
        public Entidad Entidad { get; set; } = null!;

        [ForeignKey(nameof(inmueble_id))]
        [JsonIgnore]
        public Inmueble Inmueble { get; set; } = null!;
    }
}
