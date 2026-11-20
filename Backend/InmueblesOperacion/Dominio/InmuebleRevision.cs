using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("inmueble_revision", Schema = "operacion")]
    public class InmuebleRevision
    {
        [Key]
        public Guid id { get; set; }

        public Guid inmueble_id { get; set; }

        public string decision { get; set; } = null!;

        public string? observacion { get; set; }

        public Guid revisor_ref { get; set; }

        public DateTimeOffset revisado_en { get; set; }

        [ForeignKey(nameof(inmueble_id))]
        [JsonIgnore]
        public Inmueble Inmueble { get; set; } = null!;
    }
}
