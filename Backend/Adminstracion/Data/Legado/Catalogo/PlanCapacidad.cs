using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adminstracion.Data.Legado.Catalogo
{
    [Table("plan_capacidad", Schema = "admin")]
    [PrimaryKey(nameof(plan_id), nameof(plan_version), nameof(capacidad_codigo))]
    public class PlanCapacidad
    {
        public Guid plan_id { get; set; }

        public int plan_version { get; set; }

        public string capacidad_codigo { get; set; } = null!;

        public int? limite { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(plan_id) + "," + nameof(plan_version))]
        [JsonIgnore]
        public PlanVersion PlanVersionReferencia { get; set; } = null!;

        [ForeignKey(nameof(capacidad_codigo))]
        [JsonIgnore]
        public Capacidad Capacidad { get; set; } = null!;
    }
}
