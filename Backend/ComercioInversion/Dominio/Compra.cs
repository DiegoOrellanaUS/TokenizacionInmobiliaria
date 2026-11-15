using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("compra", Schema = "comercio")]
    public class Compra
    {
        [Key]
        public Guid id { get; set; }

        public Guid ejecucion_id { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(ejecucion_id))]
        [JsonIgnore]
        public Ejecucion Ejecucion { get; set; } = null!;
    }
}
