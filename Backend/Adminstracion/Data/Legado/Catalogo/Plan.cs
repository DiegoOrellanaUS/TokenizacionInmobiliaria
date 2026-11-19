using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adminstracion.Data.Legado.Catalogo
{
    [Table("plan", Schema = "admin")]
    public class Plan
    {
        [Key]
        public Guid id { get; set; }

        public string codigo { get; set; } = null!;

        public string nombre { get; set; } = null!;

        public string? descripcion { get; set; }

        public string estado { get; set; } = null!;
    }
}
