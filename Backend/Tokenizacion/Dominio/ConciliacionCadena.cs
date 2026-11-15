using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tokenizacion.Dominio
{
    [Table("conciliacion_cadena", Schema = "tokenizacion")]
    public class ConciliacionCadena
    {
        [Key]
        public Guid id { get; set; }

        public Guid activo_id { get; set; }

        public DateTimeOffset? corte { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(activo_id))]
        [JsonIgnore]
        public ActivoCadena Activo { get; set; } = null!;
    }
}
