using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Reportes.Dominio
{
    [Table("compra_mensual_version", Schema = "reportes")]
    [PrimaryKey(nameof(mes), nameof(moneda), nameof(version))]
    public class CompraMensualVersion
    {
        public DateOnly mes { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public long version { get; set; }

        public long cantidad_compras { get; set; }

        public decimal importe_neto { get; set; }

        public DateTimeOffset? calculado_en { get; set; }

        [ForeignKey(nameof(mes) + "," + nameof(moneda))]
        [JsonIgnore]
        public CompraMensual CompraMensual { get; set; } = null!;
    }
}
