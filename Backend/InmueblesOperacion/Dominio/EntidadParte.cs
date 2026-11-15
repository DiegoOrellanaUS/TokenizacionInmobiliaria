using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("entidad_parte", Schema = "operacion")]
    [PrimaryKey(nameof(entidad_id), nameof(parte_id), nameof(fecha_creacion))]
    public class EntidadParte
    {
        public Guid entidad_id { get; set; }

        public Guid parte_id { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(entidad_id))]
        [JsonIgnore]
        public Entidad Entidad { get; set; } = null!;

        [ForeignKey(nameof(parte_id))]
        [JsonIgnore]
        public Parte Parte { get; set; } = null!;
    }
}
