using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("cierre_evidencia", Schema = "operacion")]
    [PrimaryKey(nameof(cierre_id), nameof(version), nameof(movimiento_id), nameof(documento_id), nameof(documento_version))]
    public class CierreEvidencia
    {
        public Guid cierre_id { get; set; }

        public int version { get; set; }

        public Guid movimiento_id { get; set; }

        public Guid documento_id { get; set; }

        public int documento_version { get; set; }

        public DateTimeOffset vinculo_creado_en { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(cierre_id) + "," + nameof(version) + "," + nameof(movimiento_id))]
        [JsonIgnore]
        public CierreLinea CierreLinea { get; set; } = null!;

        [ForeignKey(nameof(movimiento_id) + "," + nameof(documento_id) + "," + nameof(vinculo_creado_en))]
        [JsonIgnore]
        public MovimientoDocumento MovimientoDocumento { get; set; } = null!;

        [ForeignKey(nameof(documento_id) + "," + nameof(documento_version))]
        [JsonIgnore]
        public DocumentoVersion DocumentoVersionReferencia { get; set; } = null!;
    }
}
