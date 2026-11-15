using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Global.Dominio
{
    [Table("compra_mensual_cliente_version", Schema = "global")]
    [PrimaryKey(nameof(cliente_ref), nameof(mes), nameof(moneda), nameof(version_origen))]
    public class CompraMensualClienteVersion
    {
        public Guid cliente_ref { get; set; }

        public DateOnly mes { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public long version_origen { get; set; }

        public long cantidad_compras { get; set; }

        public decimal importe_neto { get; set; }

        public DateTimeOffset? calculado_en_origen { get; set; }

        public DateTimeOffset? recibido_en { get; set; }

        [ForeignKey(nameof(cliente_ref) + "," + nameof(mes) + "," + nameof(moneda))]
        [JsonIgnore]
        public CompraMensualCliente CompraMensualCliente { get; set; } = null!;
    }
}
