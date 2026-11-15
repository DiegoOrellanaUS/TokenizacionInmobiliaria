using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("valuacion_version", Schema = "comercio")]
    [PrimaryKey(nameof(valuacion_id), nameof(version))]
    public class ValuacionVersion
    {
        public Guid valuacion_id { get; set; }

        public int version { get; set; }

        public long? unidades_circulacion_base { get; set; }

        public DateTimeOffset? publicado_en { get; set; }

        [ForeignKey(nameof(valuacion_id))]
        [JsonIgnore]
        public ValuacionInstrumento Valuacion { get; set; } = null!;
    }
}
