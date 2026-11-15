using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Reportes.Dominio
{
    [Table("compra_mensual", Schema = "reportes")]
    [PrimaryKey(nameof(mes), nameof(moneda))]
    public class CompraMensual
    {
        public DateOnly mes { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public long? version_vigente { get; set; }

        [ForeignKey(nameof(mes) + "," + nameof(moneda) + "," + nameof(version_vigente))]
        [JsonIgnore]
        public CompraMensualVersion? CompraMensualVersion { get; set; }
    }
}
