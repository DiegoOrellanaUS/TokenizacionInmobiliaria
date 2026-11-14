using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adminstracion.Dominio
{
    public class Subscripcion
    {
        [Key]
        public int SubscripcionId { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int Precio { get; set; }
        public string CodigoPlan { get; set; }
        [ForeignKey(nameof(Usuario))]
        public int Usuarioid { get; set; }
        [JsonIgnore]
        public Usuario Usuario { get; set; }
        public string Estado { get; set; }
    }
}
