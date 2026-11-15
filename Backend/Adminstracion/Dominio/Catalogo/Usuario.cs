using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adminstracion.Dominio.Catalogo
{
    [Table("usuario", Schema = "admin")]
    public class Usuario
    {
        [Key]
        public Guid id { get; set; }

        public string sujeto_oidc { get; set; } = null!;

        public string nombre { get; set; } = null!;

        public string estado { get; set; } = null!;
    }
}
