using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adminstracion.Dominio.Catalogo
{
    [Table("servicio", Schema = "admin")]
    public class Servicio
    {
        [Key]
        public string codigo { get; set; } = null!;

        public string? nombre { get; set; }

        public string estado { get; set; } = null!;
    }
}
