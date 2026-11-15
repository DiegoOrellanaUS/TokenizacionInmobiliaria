using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Reportes.Dominio
{
    [Table("reporte_compra_mensual", Schema = "reportes")]
    [PrimaryKey(nameof(reporte_id), nameof(mes), nameof(moneda))]
    public class ReporteCompraMensual
    {
        public Guid reporte_id { get; set; }

        public DateOnly mes { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public long version_total { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(reporte_id))]
        [JsonIgnore]
        public ReporteEmitido Reporte { get; set; } = null!;

        [ForeignKey(nameof(mes) + "," + nameof(moneda) + "," + nameof(version_total))]
        [JsonIgnore]
        public CompraMensualVersion CompraMensualVersion { get; set; } = null!;
    }
}
