using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("oferta", Schema = "comercio")]
    public class Oferta
    {
        [Key]
        public Guid id { get; set; }

        public Guid instrumento_id { get; set; }

        public Guid oferente_id { get; set; }

        public string? clase { get; set; }

        public string estado { get; set; } = null!;

        [MaxLength(12)]
        public string? moneda { get; set; }

        [ForeignKey(nameof(instrumento_id))]
        [JsonIgnore]
        public Instrumento Instrumento { get; set; } = null!;

        [ForeignKey(nameof(oferente_id))]
        [JsonIgnore]
        public Participante Oferente { get; set; } = null!;
    }
}
