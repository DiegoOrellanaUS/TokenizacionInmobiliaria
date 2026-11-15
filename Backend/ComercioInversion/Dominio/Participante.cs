using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComercioInversion.Dominio
{
    [Table("participante", Schema = "comercio")]
    public class Participante
    {
        [Key]
        public Guid id { get; set; }

        public Guid? usuario_ref { get; set; }

        public Guid? parte_ref { get; set; }

        public string estado { get; set; } = null!;
    }
}
