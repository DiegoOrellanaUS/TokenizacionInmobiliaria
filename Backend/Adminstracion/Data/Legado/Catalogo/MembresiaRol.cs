using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Adminstracion.Data.Legado.Catalogo
{
    [Table("membresia_rol", Schema = "admin")]
    [PrimaryKey(nameof(membresia_id), nameof(rol_id), nameof(fecha_creacion))]
    public class MembresiaRol
    {
        public Guid membresia_id { get; set; }

        public Guid rol_id { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(membresia_id))]
        [JsonIgnore]
        public Membresia Membresia { get; set; } = null!;

        [ForeignKey(nameof(rol_id))]
        [JsonIgnore]
        public Rol Rol { get; set; } = null!;
    }
}
