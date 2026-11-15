using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("distribucion_linea", Schema = "comercio")]
    [PrimaryKey(nameof(distribucion_id), nameof(participante_id))]
    public class DistribucionLinea
    {
        public Guid distribucion_id { get; set; }

        public Guid participante_id { get; set; }

        public long? unidades_corte_base { get; set; }

        public decimal? importe_neto { get; set; }

        public Guid? asiento_ref { get; set; }

        public decimal? importe_bruto { get; set; }

        public decimal? retencion { get; set; }

        public Guid? instruccion_pago_ref { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(distribucion_id))]
        [JsonIgnore]
        public Distribucion Distribucion { get; set; } = null!;

        [ForeignKey(nameof(participante_id))]
        [JsonIgnore]
        public Participante Participante { get; set; } = null!;
    }
}
