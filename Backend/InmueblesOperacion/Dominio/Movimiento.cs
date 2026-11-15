using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("movimiento", Schema = "operacion")]
    public class Movimiento
    {
        [Key]
        public Guid id { get; set; }

        public Guid inmueble_id { get; set; }

        public DateTimeOffset? creado_en { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(inmueble_id))]
        [JsonIgnore]
        public Inmueble Inmueble { get; set; } = null!;
    }
}
