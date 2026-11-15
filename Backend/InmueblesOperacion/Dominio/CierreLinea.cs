using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("cierre_linea", Schema = "operacion")]
    [PrimaryKey(nameof(cierre_id), nameof(version), nameof(movimiento_id))]
    public class CierreLinea
    {
        public Guid cierre_id { get; set; }

        public int version { get; set; }

        public Guid movimiento_id { get; set; }

        public int movimiento_version { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(cierre_id) + "," + nameof(version))]
        [JsonIgnore]
        public CierreVersion CierreVersion { get; set; } = null!;

        [ForeignKey(nameof(movimiento_id) + "," + nameof(movimiento_version))]
        [JsonIgnore]
        public MovimientoVersion MovimientoVersionReferencia { get; set; } = null!;
    }
}
