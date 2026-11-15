using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adminstracion.Dominio.Catalogo
{
    [Table("plan_version", Schema = "admin")]
    [PrimaryKey(nameof(plan_id), nameof(version))]
    public class PlanVersion
    {
        public Guid plan_id { get; set; }

        public int version { get; set; }

        public decimal? precio { get; set; }

        [MaxLength(12)]
        public string? moneda { get; set; }

        public string? periodicidad { get; set; }

        [ForeignKey(nameof(plan_id))]
        [JsonIgnore]
        public Plan Plan { get; set; } = null!;
    }
}
