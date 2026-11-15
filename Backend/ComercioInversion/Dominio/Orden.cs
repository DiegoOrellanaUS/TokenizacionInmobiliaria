using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("orden", Schema = "comercio")]
    public class Orden
    {
        [Key]
        public Guid id { get; set; }

        public Guid participante_id { get; set; }

        public Guid oferta_id { get; set; }

        public int oferta_version { get; set; }

        public string? lado { get; set; }

        public decimal? cantidad { get; set; }

        public decimal? precio_limite { get; set; }

        public string estado { get; set; } = null!;

        public string? idempotencia { get; set; }

        [ForeignKey(nameof(participante_id))]
        [JsonIgnore]
        public Participante Participante { get; set; } = null!;

        [ForeignKey(nameof(oferta_id) + "," + nameof(oferta_version))]
        [JsonIgnore]
        public OfertaVersion OfertaVersionReferencia { get; set; } = null!;
    }
}
