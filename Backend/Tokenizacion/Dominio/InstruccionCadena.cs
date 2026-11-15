using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tokenizacion.Dominio
{
    [Table("instruccion_cadena", Schema = "tokenizacion")]
    public class InstruccionCadena
    {
        [Key]
        public Guid id { get; set; }

        public Guid activo_id { get; set; }

        public Guid? origen_id { get; set; }

        public Guid? destino_id { get; set; }

        public Guid? liquidacion_ref { get; set; }

        public string? clase { get; set; }

        public long? cantidad_base { get; set; }

        public string? idempotencia { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(activo_id))]
        [JsonIgnore]
        public ActivoCadena Activo { get; set; } = null!;

        [ForeignKey(nameof(origen_id))]
        [JsonIgnore]
        public Billetera? Origen { get; set; }

        [ForeignKey(nameof(destino_id))]
        [JsonIgnore]
        public Billetera? Destino { get; set; }
    }
}
