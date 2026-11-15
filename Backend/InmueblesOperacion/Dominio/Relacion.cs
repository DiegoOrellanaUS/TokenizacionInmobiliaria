using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("relacion", Schema = "operacion")]
    [PrimaryKey(nameof(tipo_relacion_id), nameof(origen_id), nameof(destino_id), nameof(desde))]
    public class Relacion
    {
        public Guid tipo_relacion_id { get; set; }

        public Guid origen_id { get; set; }

        public Guid destino_id { get; set; }

        public DateTimeOffset desde { get; set; }

        public DateTimeOffset? hasta { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(tipo_relacion_id))]
        [JsonIgnore]
        public TipoRelacion TipoRelacion { get; set; } = null!;

        [ForeignKey(nameof(origen_id))]
        [JsonIgnore]
        public Entidad Origen { get; set; } = null!;

        [ForeignKey(nameof(destino_id))]
        [JsonIgnore]
        public Entidad Destino { get; set; } = null!;
    }
}
