using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("cierre_version", Schema = "operacion")]
    [PrimaryKey(nameof(cierre_id), nameof(version))]
    public class CierreVersion
    {
        public Guid cierre_id { get; set; }

        public int version { get; set; }

        public Guid? creado_por_ref { get; set; }

        public DateTimeOffset? fecha { get; set; }

        public string estado { get; set; } = null!;

        [ForeignKey(nameof(cierre_id))]
        [JsonIgnore]
        public Cierre Cierre { get; set; } = null!;
    }
}
