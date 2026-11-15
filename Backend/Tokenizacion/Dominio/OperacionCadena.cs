using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tokenizacion.Dominio
{
    [Table("operacion_cadena", Schema = "tokenizacion")]
    [PrimaryKey(nameof(transaccion_id), nameof(indice))]
    public class OperacionCadena
    {
        public Guid transaccion_id { get; set; }

        public int indice { get; set; }

        public Guid? instruccion_id { get; set; }

        public Guid activo_id { get; set; }

        public Guid? origen_id { get; set; }

        public Guid? destino_id { get; set; }

        public string? clase { get; set; }

        public long? cantidad_base { get; set; }

        [ForeignKey(nameof(transaccion_id))]
        [JsonIgnore]
        public TransaccionCadena Transaccion { get; set; } = null!;

        [ForeignKey(nameof(instruccion_id))]
        [JsonIgnore]
        public InstruccionCadena? Instruccion { get; set; }

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
