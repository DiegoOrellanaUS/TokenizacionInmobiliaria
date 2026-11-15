using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("kyc_decision", Schema = "comercio")]
    public class KycDecision
    {
        [Key]
        public Guid id { get; set; }

        public Guid caso_id { get; set; }

        public Guid? revisor_ref { get; set; }

        public string? decision { get; set; }

        public DateTimeOffset? fecha { get; set; }

        [ForeignKey(nameof(caso_id))]
        [JsonIgnore]
        public KycCaso Caso { get; set; } = null!;
    }
}
