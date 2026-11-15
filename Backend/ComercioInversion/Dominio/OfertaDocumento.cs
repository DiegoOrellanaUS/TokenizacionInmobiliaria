using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("oferta_documento", Schema = "comercio")]
    [PrimaryKey(nameof(oferta_id), nameof(oferta_version), nameof(documento_ref), nameof(documento_version))]
    public class OfertaDocumento
    {
        public Guid oferta_id { get; set; }

        public int oferta_version { get; set; }

        public Guid documento_ref { get; set; }

        public int documento_version { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(oferta_id) + "," + nameof(oferta_version))]
        [JsonIgnore]
        public OfertaVersion OfertaVersionReferencia { get; set; } = null!;
    }
}
