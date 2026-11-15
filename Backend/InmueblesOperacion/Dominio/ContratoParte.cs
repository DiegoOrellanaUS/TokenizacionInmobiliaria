using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("contrato_parte", Schema = "operacion")]
    [PrimaryKey(nameof(contrato_id), nameof(parte_id), nameof(rol), nameof(fecha_creacion))]
    public class ContratoParte
    {
        public Guid contrato_id { get; set; }

        public Guid parte_id { get; set; }

        public string rol { get; set; } = null!;

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(contrato_id))]
        [JsonIgnore]
        public Contrato Contrato { get; set; } = null!;

        [ForeignKey(nameof(parte_id))]
        [JsonIgnore]
        public Parte Parte { get; set; } = null!;
    }
}
