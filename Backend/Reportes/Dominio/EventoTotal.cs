using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Reportes.Dominio
{
    [Table("evento_total", Schema = "reportes")]
    public class EventoTotal
    {
        [Key]
        public Guid id { get; set; }

        public DateOnly mes { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public long version_total { get; set; }

        public DateTimeOffset? creado_en { get; set; }

        public DateTimeOffset? publicado_en { get; set; }

        [ForeignKey(nameof(mes) + "," + nameof(moneda) + "," + nameof(version_total))]
        [JsonIgnore]
        public CompraMensualVersion CompraMensualVersion { get; set; } = null!;
    }
}
