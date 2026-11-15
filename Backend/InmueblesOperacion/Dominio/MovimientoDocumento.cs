using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("movimiento_documento", Schema = "operacion")]
    [PrimaryKey(nameof(movimiento_id), nameof(documento_id), nameof(fecha_creacion))]
    public class MovimientoDocumento
    {
        public Guid movimiento_id { get; set; }

        public Guid documento_id { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(movimiento_id))]
        [JsonIgnore]
        public Movimiento Movimiento { get; set; } = null!;

        [ForeignKey(nameof(documento_id))]
        [JsonIgnore]
        public Documento Documento { get; set; } = null!;
    }
}
