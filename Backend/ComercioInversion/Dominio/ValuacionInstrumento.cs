using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("valuacion_instrumento", Schema = "comercio")]
    public class ValuacionInstrumento
    {
        [Key]
        public Guid id { get; set; }

        public Guid instrumento_id { get; set; }

        public DateOnly? fecha_corte { get; set; }

        [MaxLength(12)]
        public string? moneda { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(instrumento_id))]
        [JsonIgnore]
        public Instrumento Instrumento { get; set; } = null!;
    }
}
