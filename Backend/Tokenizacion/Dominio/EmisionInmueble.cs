using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tokenizacion.Dominio;

[Table("emision_inmueble", Schema = "tokenizacion")]
public class EmisionInmueble
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid inmueble_ref { get; set; }

    public Guid instruccion_id { get; set; }
    public Guid creado_por_ref { get; set; }
    public DateTimeOffset creada_en { get; set; }
    public Guid? transaccion_id { get; set; }

    [JsonIgnore]
    public string? sobre_firmado { get; set; }
    public string? hash { get; set; }

    public InstruccionCadena Instruccion { get; set; } = null!;
    public TransaccionCadena? Transaccion { get; set; }
}
