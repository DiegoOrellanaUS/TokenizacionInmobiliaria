using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Reportes.Dominio
{
    [Table("reporte_emitido", Schema = "reportes")]
    public class ReporteEmitido
    {
        [Key]
        public Guid id { get; set; }

        public Guid plantilla_id { get; set; }

        public int plantilla_version { get; set; }

        public Guid? solicitado_por_ref { get; set; }

        public string? formato { get; set; }

        public string? objeto { get; set; }

        public DateTimeOffset? generado_en { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(plantilla_id) + "," + nameof(plantilla_version))]
        [JsonIgnore]
        public PlantillaVersion PlantillaVersionReferencia { get; set; } = null!;
    }
}
