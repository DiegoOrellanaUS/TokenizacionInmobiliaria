using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("distribucion", Schema = "comercio")]
    public class Distribucion
    {
        [Key]
        public Guid id { get; set; }

        public Guid instrumento_id { get; set; }

        public DateTimeOffset? fecha_corte { get; set; }

        [MaxLength(12)]
        public string? moneda { get; set; }

        public string estado { get; set; } = null!;

        public Guid? instantanea_ref { get; set; }

        [MaxLength(12)]
        public string? moneda_origen { get; set; }

        public decimal? tipo_cambio_referencia { get; set; }

        public string? fuente_tipo_cambio { get; set; }

        public Guid? conversion_ref { get; set; }

        public string? informe_hash { get; set; }

        [ForeignKey(nameof(instrumento_id))]
        [JsonIgnore]
        public Instrumento Instrumento { get; set; } = null!;
    }
}
