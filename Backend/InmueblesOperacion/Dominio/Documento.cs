using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InmueblesOperacion.Dominio
{
    [Table("documento", Schema = "operacion")]
    public class Documento
    {
        [Key]
        public Guid id { get; set; }

        public string? clase { get; set; }

        public Guid? creado_por_ref { get; set; }

        public string estado { get; set; } = null!;
    }
}
