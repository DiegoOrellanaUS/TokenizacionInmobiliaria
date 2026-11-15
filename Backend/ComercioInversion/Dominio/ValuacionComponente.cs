using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ComercioInversion.Dominio
{
    [Table("valuacion_componente", Schema = "comercio")]
    [PrimaryKey(nameof(valuacion_id), nameof(version), nameof(concepto))]
    public class ValuacionComponente
    {
        public Guid valuacion_id { get; set; }

        public int version { get; set; }

        public string concepto { get; set; } = null!;

        public decimal? importe_neto { get; set; }

        [ForeignKey(nameof(valuacion_id) + "," + nameof(version))]
        [JsonIgnore]
        public ValuacionVersion ValuacionVersion { get; set; } = null!;
    }
}
