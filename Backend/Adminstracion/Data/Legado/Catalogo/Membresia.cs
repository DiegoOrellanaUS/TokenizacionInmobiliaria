using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adminstracion.Data.Legado.Catalogo
{
    [Table("membresia", Schema = "admin")]
    public class Membresia
    {
        [Key]
        public Guid id { get; set; }

        public Guid cliente_id { get; set; }

        public Guid usuario_id { get; set; }

        public string estado { get; set; } = null!;

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(cliente_id))]
        [JsonIgnore]
        public Cliente Cliente { get; set; } = null!;

        [ForeignKey(nameof(usuario_id))]
        [JsonIgnore]
        public Usuario Usuario { get; set; } = null!;
    }
}
