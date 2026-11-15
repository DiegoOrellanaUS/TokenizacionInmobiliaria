using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("contrato_documento", Schema = "operacion")]
    [PrimaryKey(nameof(contrato_id), nameof(documento_id), nameof(fecha_creacion))]
    public class ContratoDocumento
    {
        public Guid contrato_id { get; set; }

        public Guid documento_id { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(contrato_id))]
        [JsonIgnore]
        public Contrato Contrato { get; set; } = null!;

        [ForeignKey(nameof(documento_id))]
        [JsonIgnore]
        public Documento Documento { get; set; } = null!;
    }
}
