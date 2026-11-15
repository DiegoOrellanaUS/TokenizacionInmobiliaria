using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("evento_compra", Schema = "comercio")]
    public class EventoCompra
    {
        [Key]
        public Guid id { get; set; }

        public Guid compra_id { get; set; }

        public long compra_version { get; set; }

        public string? tipo { get; set; }

        public DateTimeOffset? creado_en { get; set; }

        public DateTimeOffset? publicado_en { get; set; }

        [ForeignKey(nameof(compra_id) + "," + nameof(compra_version))]
        [JsonIgnore]
        public CompraVersion CompraVersionReferencia { get; set; } = null!;
    }
}
