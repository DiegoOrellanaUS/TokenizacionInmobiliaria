using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InmueblesOperacion.Dominio
{
    [Table("auditoria", Schema = "operacion")]
    public class Auditoria
    {
        [Key]
        public Guid id { get; set; }

        public Guid? actor_ref { get; set; }

        public string? accion { get; set; }

        public string? recurso { get; set; }

        public Guid? objeto_id { get; set; }

        public DateTimeOffset? fecha { get; set; }

        public Guid? correlacion { get; set; }
    }
}
