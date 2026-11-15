using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("valor_atributo", Schema = "operacion")]
    [PrimaryKey(nameof(entidad_id), nameof(atributo_id))]
    public class ValorAtributo
    {
        public Guid entidad_id { get; set; }

        public Guid atributo_id { get; set; }

        public string? valor_texto { get; set; }

        public decimal? valor_numero { get; set; }

        public DateOnly? valor_fecha { get; set; }

        public bool? valor_booleano { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(entidad_id))]
        [JsonIgnore]
        public Entidad Entidad { get; set; } = null!;

        [ForeignKey(nameof(atributo_id))]
        [JsonIgnore]
        public Atributo Atributo { get; set; } = null!;
    }
}
