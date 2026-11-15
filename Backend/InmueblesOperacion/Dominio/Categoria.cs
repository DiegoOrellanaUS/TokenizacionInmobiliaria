using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InmueblesOperacion.Dominio
{
    [Table("categoria", Schema = "operacion")]
    public class Categoria
    {
        [Key]
        public Guid id { get; set; }

        public string? codigo { get; set; }

        public string? nombre { get; set; }

        public string? naturaleza { get; set; }

        public string estado { get; set; } = null!;
    }
}
