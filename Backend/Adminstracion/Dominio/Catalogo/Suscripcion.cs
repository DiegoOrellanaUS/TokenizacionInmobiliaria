using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adminstracion.Dominio.Catalogo
{
    [Table("suscripcion", Schema = "admin")]
    public class Suscripcion
    {
        [Key]
        public Guid id { get; set; }

        public Guid cliente_id { get; set; }

        public Guid plan_id { get; set; }

        public int plan_version { get; set; }

        public string estado { get; set; } = null!;

        public DateTimeOffset? desde { get; set; }

        public DateTimeOffset? hasta { get; set; }

        [ForeignKey(nameof(cliente_id))]
        [JsonIgnore]
        public Cliente Cliente { get; set; } = null!;

        [ForeignKey(nameof(plan_id) + "," + nameof(plan_version))]
        [JsonIgnore]
        public PlanVersion PlanVersionReferencia { get; set; } = null!;
    }
}
