using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InmueblesOperacion.Dominio
{
    [Table("tipo_entidad", Schema = "operacion")]
    public class TipoEntidad
    {
        [Key]
        public Guid id { get; set; }

        public string? codigo { get; set; }

        public string? nombre { get; set; }

        public string estado { get; set; } = null!;
    }
}
