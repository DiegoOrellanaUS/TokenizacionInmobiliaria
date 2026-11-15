using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tokenizacion.Dominio
{
    [Table("instantanea_saldo_linea", Schema = "tokenizacion")]
    [PrimaryKey(nameof(instantanea_id), nameof(billetera_id))]
    public class InstantaneaSaldoLinea
    {
        public Guid instantanea_id { get; set; }

        public Guid billetera_id { get; set; }

        public long? cantidad_base { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(instantanea_id))]
        [JsonIgnore]
        public InstantaneaSaldo Instantanea { get; set; } = null!;

        [ForeignKey(nameof(billetera_id))]
        [JsonIgnore]
        public Billetera Billetera { get; set; } = null!;
    }
}
