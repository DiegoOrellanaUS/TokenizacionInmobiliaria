using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adminstracion.Dominio.Catalogo
{
    [Table("capacidad", Schema = "admin")]
    public class Capacidad
    {
        [Key]
        public string codigo { get; set; } = null!;

        public string? descripcion { get; set; }

        public string nombre_visible { get; set; } = null!;

        public string estado { get; set; } = null!;
    }
}
