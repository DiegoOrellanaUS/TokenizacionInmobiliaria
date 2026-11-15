using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("valuacion_avaluo", Schema = "comercio")]
    [PrimaryKey(nameof(valuacion_id), nameof(version), nameof(avaluo_ref))]
    public class ValuacionAvaluo
    {
        public Guid valuacion_id { get; set; }

        public int version { get; set; }

        public Guid avaluo_ref { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(valuacion_id) + "," + nameof(version))]
        [JsonIgnore]
        public ValuacionVersion ValuacionVersion { get; set; } = null!;
    }
}
