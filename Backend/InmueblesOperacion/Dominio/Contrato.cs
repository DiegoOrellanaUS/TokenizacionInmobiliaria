using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("contrato", Schema = "operacion")]
    public class Contrato
    {
        [Key]
        public Guid id { get; set; }

        public Guid unidad_id { get; set; }

        public DateOnly? inicio { get; set; }

        public DateOnly? fin { get; set; }

        [MaxLength(12)]
        public string? moneda { get; set; }

        public decimal? renta { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(unidad_id))]
        [JsonIgnore]
        public Unidad Unidad { get; set; } = null!;
    }
}
