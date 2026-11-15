using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("distribucion_cierre", Schema = "comercio")]
    [PrimaryKey(nameof(distribucion_id), nameof(cierre_ref), nameof(cierre_version))]
    public class DistribucionCierre
    {
        public Guid distribucion_id { get; set; }

        public Guid cierre_ref { get; set; }

        public int cierre_version { get; set; }

        public decimal? importe_aplicado { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(distribucion_id))]
        [JsonIgnore]
        public Distribucion Distribucion { get; set; } = null!;
    }
}
