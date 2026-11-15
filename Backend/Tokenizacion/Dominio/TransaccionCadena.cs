using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokenizacion.Dominio
{
    [Table("transaccion_cadena", Schema = "tokenizacion")]
    public class TransaccionCadena
    {
        [Key]
        public Guid id { get; set; }

        public string red { get; set; } = null!;

        public string hash { get; set; } = null!;

        public DateTimeOffset? confirmada_en { get; set; }

        public string estado { get; set; } = null!;

        public long? numero_ledger { get; set; }
    }
}
