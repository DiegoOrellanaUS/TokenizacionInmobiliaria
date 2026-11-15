using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reportes.Dominio
{
    [Table("plantilla_version", Schema = "reportes")]
    [PrimaryKey(nameof(plantilla_id), nameof(version))]
    public class PlantillaVersion
    {
        public Guid plantilla_id { get; set; }

        public int version { get; set; }

        public JsonDocument? definicion { get; set; }

        public DateTimeOffset? creada_en { get; set; }

        [ForeignKey(nameof(plantilla_id))]
        [JsonIgnore]
        public Plantilla Plantilla { get; set; } = null!;
    }
}
