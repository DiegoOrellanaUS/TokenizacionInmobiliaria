using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("permiso_inmueble", Schema = "operacion")]
    [PrimaryKey(nameof(usuario_ref), nameof(inmueble_id), nameof(permiso), nameof(fecha_creacion))]
    public class PermisoInmueble
    {
        public Guid usuario_ref { get; set; }

        public Guid inmueble_id { get; set; }

        public string permiso { get; set; } = null!;

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(inmueble_id))]
        [JsonIgnore]
        public Inmueble Inmueble { get; set; } = null!;
    }
}
