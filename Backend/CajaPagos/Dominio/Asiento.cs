using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CajaPagos.Dominio
{
    [Table("asiento", Schema = "caja")]
    public class Asiento
    {
        [Key]
        public Guid id { get; set; }

        public DateTimeOffset? registrado_en { get; set; }

        public string estado { get; set; } = null!;

        public string? idempotencia { get; set; }
    }
}
