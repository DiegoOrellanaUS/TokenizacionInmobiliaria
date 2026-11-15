using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("liquidacion", Schema = "comercio")]
    public class Liquidacion
    {
        [Key]
        public Guid id { get; set; }

        public Guid ejecucion_id { get; set; }

        public Guid? asiento_ref { get; set; }

        public Guid? instruccion_cadena_ref { get; set; }

        public string estado { get; set; } = null!;

        public DateTimeOffset? fecha { get; set; }

        public Guid? conversion_ref { get; set; }

        [ForeignKey(nameof(ejecucion_id))]
        [JsonIgnore]
        public Ejecucion Ejecucion { get; set; } = null!;
    }
}
