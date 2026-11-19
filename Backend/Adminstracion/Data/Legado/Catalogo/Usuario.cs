using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adminstracion.Data.Legado.Catalogo
{
    [Table("usuario", Schema = "admin")]
    public class Usuario
    {
        [Key]
        public Guid id { get; set; }

        public string sujeto_oidc { get; set; } = null!;

        public string nombre { get; set; } = null!;

        public string? ci { get; set; }

        public string? correo_electronico { get; set; }

        [JsonIgnore]
        public string? contrasenia_hash { get; set; }

        public string estado { get; set; } = null!;
    }
}
