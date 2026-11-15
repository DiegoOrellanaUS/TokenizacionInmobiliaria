using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Global.Dominio
{
    [Table("compra_mensual_global_version", Schema = "global")]
    [PrimaryKey(nameof(mes), nameof(moneda), nameof(version))]
    public class CompraMensualGlobalVersion
    {
        public DateOnly mes { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public long version { get; set; }

        public long cantidad_compras { get; set; }

        public decimal importe_neto { get; set; }

        public DateTimeOffset? calculado_en { get; set; }

        [ForeignKey(nameof(mes) + "," + nameof(moneda))]
        [JsonIgnore]
        public CompraMensualGlobal CompraMensualGlobal { get; set; } = null!;
    }
}
