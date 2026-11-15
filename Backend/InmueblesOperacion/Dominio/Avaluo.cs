using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("avaluo", Schema = "operacion")]
    public class Avaluo
    {
        [Key]
        public Guid id { get; set; }

        public Guid inmueble_id { get; set; }

        public Guid tasador_id { get; set; }

        public string? metodo { get; set; }

        public DateOnly? fecha_valuacion { get; set; }

        public decimal? importe { get; set; }

        [MaxLength(12)]
        public string? moneda { get; set; }

        public Guid documento_id { get; set; }

        public int documento_version { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(inmueble_id))]
        [JsonIgnore]
        public Inmueble Inmueble { get; set; } = null!;

        [ForeignKey(nameof(tasador_id))]
        [JsonIgnore]
        public Parte Tasador { get; set; } = null!;

        [ForeignKey(nameof(documento_id) + "," + nameof(documento_version))]
        [JsonIgnore]
        public DocumentoVersion DocumentoVersionReferencia { get; set; } = null!;
    }
}
