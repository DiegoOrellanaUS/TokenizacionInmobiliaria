using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("ejecucion", Schema = "comercio")]
    public class Ejecucion
    {
        [Key]
        public Guid id { get; set; }

        public Guid orden_compra_id { get; set; }

        public Guid? orden_venta_id { get; set; }

        public decimal? cantidad { get; set; }

        public decimal? precio { get; set; }

        public DateTimeOffset? fecha { get; set; }

        [ForeignKey(nameof(orden_compra_id))]
        [JsonIgnore]
        public Orden OrdenCompra { get; set; } = null!;

        [ForeignKey(nameof(orden_venta_id))]
        [JsonIgnore]
        public Orden? OrdenVenta { get; set; }
    }
}
