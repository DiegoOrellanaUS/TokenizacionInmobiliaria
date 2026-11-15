using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reportes.Dominio
{
    [Table("plantilla", Schema = "reportes")]
    public class Plantilla
    {
        [Key]
        public Guid id { get; set; }

        public string? codigo { get; set; }

        public string? nombre { get; set; }

        public string estado { get; set; } = null!;
    }
}
