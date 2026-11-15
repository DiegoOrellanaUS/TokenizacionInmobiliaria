using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("cierre", Schema = "operacion")]
    public class Cierre
    {
        [Key]
        public Guid id { get; set; }

        public Guid inmueble_id { get; set; }

        public DateOnly mes { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public int? version_publicada { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(inmueble_id))]
        [JsonIgnore]
        public Inmueble Inmueble { get; set; } = null!;

        [ForeignKey(nameof(id) + "," + nameof(version_publicada))]
        [JsonIgnore]
        public CierreVersion? CierreVersion { get; set; }
    }
}
