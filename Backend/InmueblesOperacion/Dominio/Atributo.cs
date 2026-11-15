using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("atributo", Schema = "operacion")]
    public class Atributo
    {
        [Key]
        public Guid id { get; set; }

        public Guid tipo_entidad_id { get; set; }

        public string? codigo { get; set; }

        public string? clase_valor { get; set; }

        public string? nombre_visible { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(tipo_entidad_id))]
        [JsonIgnore]
        public TipoEntidad TipoEntidad { get; set; } = null!;
    }
}
