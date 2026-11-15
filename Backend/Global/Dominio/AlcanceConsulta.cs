using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Global.Dominio
{
    [Table("alcance_consulta", Schema = "global")]
    [PrimaryKey(nameof(operador_ref), nameof(cliente_ref), nameof(permiso), nameof(fecha_creacion))]
    public class AlcanceConsulta
    {
        public Guid operador_ref { get; set; }

        public Guid cliente_ref { get; set; }

        public string permiso { get; set; } = null!;

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(cliente_ref))]
        [JsonIgnore]
        public ClienteLectura ClienteLecturaReferencia { get; set; } = null!;
    }
}
