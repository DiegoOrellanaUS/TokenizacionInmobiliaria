using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("instrumento", Schema = "comercio")]
    public class Instrumento
    {
        [Key]
        public Guid id { get; set; }

        public Guid emisor_id { get; set; }

        public string? derecho { get; set; }

        public string? unidad { get; set; }

        public string? nombre_visible { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(emisor_id))]
        [JsonIgnore]
        public Participante Emisor { get; set; } = null!;
    }
}
