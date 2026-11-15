using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("kyc_evidencia", Schema = "comercio")]
    public class KycEvidencia
    {
        [Key]
        public Guid id { get; set; }

        public Guid caso_id { get; set; }

        public string? tipo { get; set; }

        public string? objeto_privado { get; set; }

        public string? hash { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(caso_id))]
        [JsonIgnore]
        public KycCaso Caso { get; set; } = null!;
    }
}
