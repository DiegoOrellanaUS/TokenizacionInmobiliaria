using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("inmueble_documento", Schema = "operacion")]
    [PrimaryKey(nameof(inmueble_id), nameof(documento_id), nameof(fecha_creacion))]
    public class InmuebleDocumento
    {
        public Guid inmueble_id { get; set; }

        public Guid documento_id { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(inmueble_id))]
        [JsonIgnore]
        public Inmueble Inmueble { get; set; } = null!;

        [ForeignKey(nameof(documento_id))]
        [JsonIgnore]
        public Documento Documento { get; set; } = null!;
    }
}
