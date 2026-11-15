using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Global.Dominio
{
    [Table("compra_mensual_global_cambio", Schema = "global")]
    [PrimaryKey(nameof(mes), nameof(moneda), nameof(version_global), nameof(cliente_ref))]
    public class CompraMensualGlobalCambio
    {
        public DateOnly mes { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public long version_global { get; set; }

        public Guid cliente_ref { get; set; }

        public long version_origen { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(mes) + "," + nameof(moneda) + "," + nameof(version_global))]
        [JsonIgnore]
        public CompraMensualGlobalVersion CompraMensualGlobalVersion { get; set; } = null!;

        [ForeignKey(nameof(cliente_ref) + "," + nameof(mes) + "," + nameof(moneda) + "," + nameof(version_origen))]
        [JsonIgnore]
        public CompraMensualClienteVersion CompraMensualClienteVersion { get; set; } = null!;
    }
}
