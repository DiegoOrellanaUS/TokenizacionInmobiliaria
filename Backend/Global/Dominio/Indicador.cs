using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Global.Dominio
{
    [Table("indicador", Schema = "global")]
    public class Indicador
    {
        [Key]
        public string codigo { get; set; } = null!;

        public string? nombre { get; set; }

        public string? unidad { get; set; }

        public string estado { get; set; } = null!;
    }
}
