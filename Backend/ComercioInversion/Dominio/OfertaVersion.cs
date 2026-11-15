using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("oferta_version", Schema = "comercio")]
    [PrimaryKey(nameof(oferta_id), nameof(version))]
    public class OfertaVersion
    {
        public Guid oferta_id { get; set; }

        public int version { get; set; }

        public decimal? precio { get; set; }

        public decimal? unidades { get; set; }

        public DateTimeOffset? fecha { get; set; }

        [ForeignKey(nameof(oferta_id))]
        [JsonIgnore]
        public Oferta Oferta { get; set; } = null!;
    }
}
