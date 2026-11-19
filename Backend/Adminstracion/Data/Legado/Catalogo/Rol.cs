using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adminstracion.Data.Legado.Catalogo
{
    [Table("rol", Schema = "admin")]
    public class Rol
    {
        [Key]
        public Guid id { get; set; }

        public string codigo { get; set; } = null!;

        public string nombre_visible { get; set; } = null!;

        public string? descripcion { get; set; }

        public int? orden_visual { get; set; }

        public string estado { get; set; } = null!;
    }
}
