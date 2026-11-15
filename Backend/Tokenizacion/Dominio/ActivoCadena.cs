using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokenizacion.Dominio
{
    [Table("activo_cadena", Schema = "tokenizacion")]
    public class ActivoCadena
    {
        [Key]
        public Guid id { get; set; }

        public Guid? instrumento_ref { get; set; }

        public string red { get; set; } = null!;

        public string emisor { get; set; } = null!;

        public string codigo { get; set; } = null!;

        public long? oferta_maxima_base { get; set; }

        public string estado { get; set; } = null!;
    }
}
