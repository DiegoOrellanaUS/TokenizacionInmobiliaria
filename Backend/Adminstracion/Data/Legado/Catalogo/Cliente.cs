using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adminstracion.Data.Legado.Catalogo
{
    [Table("cliente", Schema = "admin")]
    public class Cliente
    {
        [Key]
        public Guid id { get; set; }

        public string codigo { get; set; } = null!;

        public string razon_social { get; set; } = null!;

        public string estado { get; set; } = null!;
    }
}
