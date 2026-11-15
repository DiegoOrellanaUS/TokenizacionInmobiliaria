using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Global.Dominio
{
    [Table("cliente_lectura", Schema = "global")]
    public class ClienteLectura
    {
        [Key]
        public Guid cliente_ref { get; set; }

        public string? nombre_mostrado { get; set; }

        public string estado { get; set; } = null!;

        public DateTimeOffset? actualizado_en { get; set; }
    }
}
