using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reportes.Dominio
{
    [Table("compra_lectura", Schema = "reportes")]
    public class CompraLectura
    {
        [Key]
        public Guid compra_ref { get; set; }

        public long version_origen { get; set; }

        public DateOnly mes { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public bool cuenta_en_total { get; set; }

        public decimal importe_neto { get; set; }

        public DateTimeOffset? actualizado_en { get; set; }
    }
}
