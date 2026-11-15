using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("unidad", Schema = "operacion")]
    public class Unidad
    {
        [Key]
        public Guid id { get; set; }

        public Guid inmueble_id { get; set; }

        public string codigo { get; set; } = null!;

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(inmueble_id))]
        [JsonIgnore]
        public Inmueble Inmueble { get; set; } = null!;
    }
}
