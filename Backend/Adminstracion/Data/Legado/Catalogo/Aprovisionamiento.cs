using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adminstracion.Data.Legado.Catalogo
{
    [Table("aprovisionamiento", Schema = "admin")]
    public class Aprovisionamiento
    {
        [Key]
        public Guid id { get; set; }

        public Guid cliente_id { get; set; }

        public string servicio_codigo { get; set; } = null!;

        public string? idempotencia { get; set; }

        public string? operacion { get; set; }

        public string estado { get; set; } = null!;

        public DateTimeOffset? iniciado_en { get; set; }

        public DateTimeOffset? finalizado_en { get; set; }

        [ForeignKey(nameof(cliente_id) + "," + nameof(servicio_codigo))]
        [JsonIgnore]
        public SiloServicio SiloServicio { get; set; } = null!;
    }
}
