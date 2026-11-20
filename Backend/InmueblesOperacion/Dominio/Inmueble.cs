using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InmueblesOperacion.Dominio
{
    [Table("inmueble", Schema = "operacion")]
    public class Inmueble
    {
        [Key]
        public Guid id { get; set; }

        public string nombre { get; set; } = null!;

        public string? direccion { get; set; }

        public string estado { get; set; } = null!;

        public string? tipo { get; set; }

        public string? ciudad { get; set; }

        public decimal superficie_m2 { get; set; }

        public string estado_revision { get; set; } = "BORRADOR";

        public Guid creado_por_ref { get; set; }
    }
}
