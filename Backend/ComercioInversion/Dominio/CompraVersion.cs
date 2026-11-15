using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("compra_version", Schema = "comercio")]
    [PrimaryKey(nameof(compra_id), nameof(version))]
    public class CompraVersion
    {
        public Guid compra_id { get; set; }

        public long version { get; set; }

        public string estado { get; set; } = null!;

        public decimal importe_neto { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public DateOnly fecha_efectiva { get; set; }

        public DateTimeOffset? registrada_en { get; set; }

        [ForeignKey(nameof(compra_id))]
        [JsonIgnore]
        public Compra Compra { get; set; } = null!;
    }
}
