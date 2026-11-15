using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("tipo_relacion", Schema = "operacion")]
    public class TipoRelacion
    {
        [Key]
        public Guid id { get; set; }

        public Guid tipo_origen_id { get; set; }

        public Guid tipo_destino_id { get; set; }

        public string? codigo { get; set; }

        public string? nombre_visible { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(tipo_origen_id))]
        [JsonIgnore]
        public TipoEntidad TipoOrigen { get; set; } = null!;

        [ForeignKey(nameof(tipo_destino_id))]
        [JsonIgnore]
        public TipoEntidad TipoDestino { get; set; } = null!;
    }
}
