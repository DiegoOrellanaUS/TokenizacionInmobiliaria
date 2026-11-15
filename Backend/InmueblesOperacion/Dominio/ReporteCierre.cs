using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("reporte_cierre", Schema = "operacion")]
    [PrimaryKey(nameof(cierre_id), nameof(version), nameof(formato))]
    public class ReporteCierre
    {
        public Guid cierre_id { get; set; }

        public int version { get; set; }

        public string formato { get; set; } = null!;

        public string? objeto { get; set; }

        public DateTimeOffset? generado_en { get; set; }

        public string? hash { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(cierre_id) + "," + nameof(version))]
        [JsonIgnore]
        public CierreVersion CierreVersion { get; set; } = null!;
    }
}
