using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("kyc_caso", Schema = "comercio")]
    public class KycCaso
    {
        [Key]
        public Guid id { get; set; }

        public Guid participante_id { get; set; }

        public string? proveedor { get; set; }

        public string? referencia_externa { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(participante_id))]
        [JsonIgnore]
        public Participante Participante { get; set; } = null!;
    }
}
