using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adminstracion.Data.Legado.Catalogo
{
    [Table("cobro_suscripcion", Schema = "admin")]
    public class CobroSuscripcion
    {
        [Key]
        public Guid id { get; set; }

        public Guid suscripcion_id { get; set; }

        public string? proveedor { get; set; }

        public string? referencia_externa { get; set; }

        public decimal? importe { get; set; }

        [MaxLength(12)]
        public string? moneda { get; set; }

        public string estado { get; set; } = null!;

        public DateTimeOffset? confirmado_en { get; set; }

        [ForeignKey(nameof(suscripcion_id))]
        [JsonIgnore]
        public Suscripcion Suscripcion { get; set; } = null!;
    }
}
