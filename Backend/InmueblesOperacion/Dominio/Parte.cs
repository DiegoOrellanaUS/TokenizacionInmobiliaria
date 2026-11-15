using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InmueblesOperacion.Dominio
{
    [Table("parte", Schema = "operacion")]
    public class Parte
    {
        [Key]
        public Guid id { get; set; }

        public string? nombre { get; set; }

        public string? clase { get; set; }

        public string estado { get; set; } = null!;
    }
}
