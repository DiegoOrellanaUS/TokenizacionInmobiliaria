using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokenizacion.Dominio
{
    [Table("billetera", Schema = "tokenizacion")]
    public class Billetera
    {
        [Key]
        public Guid id { get; set; }

        public Guid? titular_ref { get; set; }

        public string red { get; set; } = null!;

        public string direccion { get; set; } = null!;

        public string? proposito { get; set; }

        public string estado { get; set; } = null!;
    }
}
