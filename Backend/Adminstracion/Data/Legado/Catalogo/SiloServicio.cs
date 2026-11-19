using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adminstracion.Data.Legado.Catalogo
{
    [Table("silo_servicio", Schema = "admin")]
    [PrimaryKey(nameof(cliente_id), nameof(servicio_codigo))]
    public class SiloServicio
    {
        public Guid cliente_id { get; set; }

        public string servicio_codigo { get; set; } = null!;

        public string? localizador_bd { get; set; }

        public string? secreto_ref { get; set; }

        public string estado { get; set; } = null!;

        public string? version_esquema { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(cliente_id))]
        [JsonIgnore]
        public Cliente Cliente { get; set; } = null!;

        [ForeignKey(nameof(servicio_codigo))]
        [JsonIgnore]
        public Servicio Servicio { get; set; } = null!;
    }
}
