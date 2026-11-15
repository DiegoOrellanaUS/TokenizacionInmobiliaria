using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CajaPagos.Dominio
{
    [Table("conciliacion", Schema = "caja")]
    public class Conciliacion
    {
        [Key]
        public Guid id { get; set; }

        public string? proveedor { get; set; }

        public DateTimeOffset? corte { get; set; }

        public string estado { get; set; } = null!;
    }
}
