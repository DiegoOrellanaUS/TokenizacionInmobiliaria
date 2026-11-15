using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Global.Dominio
{
    [Table("compra_mensual_cliente", Schema = "global")]
    [PrimaryKey(nameof(cliente_ref), nameof(mes), nameof(moneda))]
    public class CompraMensualCliente
    {
        public Guid cliente_ref { get; set; }

        public DateOnly mes { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public long? version_vigente { get; set; }

        [ForeignKey(nameof(cliente_ref))]
        [JsonIgnore]
        public ClienteLectura ClienteLecturaReferencia { get; set; } = null!;

        [ForeignKey(nameof(cliente_ref) + "," + nameof(mes) + "," + nameof(moneda) + "," + nameof(version_vigente))]
        [JsonIgnore]
        public CompraMensualClienteVersion? CompraMensualClienteVersion { get; set; }
    }
}
