using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("documento_version", Schema = "operacion")]
    [PrimaryKey(nameof(documento_id), nameof(version))]
    public class DocumentoVersion
    {
        public Guid documento_id { get; set; }

        public int version { get; set; }

        public string objeto { get; set; } = null!;

        public string hash { get; set; } = null!;

        public DateTimeOffset fecha { get; set; }

        [ForeignKey(nameof(documento_id))]
        [JsonIgnore]
        public Documento Documento { get; set; } = null!;
    }
}
