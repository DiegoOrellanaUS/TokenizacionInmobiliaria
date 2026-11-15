using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("entidad", Schema = "operacion")]
    public class Entidad
    {
        [Key]
        public Guid id { get; set; }

        public Guid tipo_id { get; set; }

        public string? nombre { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(tipo_id))]
        [JsonIgnore]
        public TipoEntidad Tipo { get; set; } = null!;
    }
}
