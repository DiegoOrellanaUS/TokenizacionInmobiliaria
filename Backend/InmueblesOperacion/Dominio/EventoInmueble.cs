using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InmueblesOperacion.Dominio;

[Table("evento_inmueble", Schema = "operacion")]
public class EventoInmueble
{
    [Key]
    public long id { get; set; }
    public Guid inmueble_id { get; set; }
    public string estado_revision { get; set; } = null!;
    public DateTimeOffset ocurrido_en { get; set; }
    public Inmueble Inmueble { get; set; } = null!;
}
