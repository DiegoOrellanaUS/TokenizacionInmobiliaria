using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tokenizacion.Dominio
{
    [Table("instantanea_saldo", Schema = "tokenizacion")]
    public class InstantaneaSaldo
    {
        [Key]
        public Guid id { get; set; }

        public Guid activo_id { get; set; }

        public long? numero_ledger { get; set; }

        public DateTimeOffset? fecha_corte { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(activo_id))]
        [JsonIgnore]
        public ActivoCadena Activo { get; set; } = null!;
    }
}
