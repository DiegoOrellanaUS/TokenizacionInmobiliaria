using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("titularidad", Schema = "operacion")]
    [PrimaryKey(nameof(inmueble_id), nameof(parte_id), nameof(desde))]
    public class Titularidad
    {
        public Guid inmueble_id { get; set; }

        public Guid parte_id { get; set; }

        public DateOnly desde { get; set; }

        public DateOnly? hasta { get; set; }

        public decimal? cuota { get; set; }

        public DateTimeOffset? verificada_en { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(inmueble_id))]
        [JsonIgnore]
        public Inmueble Inmueble { get; set; } = null!;

        [ForeignKey(nameof(parte_id))]
        [JsonIgnore]
        public Parte Parte { get; set; } = null!;
    }
}
