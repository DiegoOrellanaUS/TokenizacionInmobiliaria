using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("usuario_parte", Schema = "operacion")]
    [PrimaryKey(nameof(usuario_ref), nameof(parte_id), nameof(fecha_creacion))]
    public class UsuarioParte
    {
        public Guid usuario_ref { get; set; }

        public Guid parte_id { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(parte_id))]
        [JsonIgnore]
        public Parte Parte { get; set; } = null!;
    }
}
