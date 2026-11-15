using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CajaPagos.Dominio
{
    [Table("cuenta", Schema = "caja")]
    public class Cuenta
    {
        [Key]
        public Guid id { get; set; }

        public Guid? titular_ref { get; set; }

        public string tipo { get; set; } = null!;

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public string estado { get; set; } = null!;
    }
}
