using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Global.Dominio
{
    [Table("indicador_cliente", Schema = "global")]
    [PrimaryKey(nameof(cliente_ref), nameof(indicador_codigo), nameof(fuente_servicio), nameof(periodo), nameof(dimension))]
    public class IndicadorCliente
    {
        public Guid cliente_ref { get; set; }

        public string indicador_codigo { get; set; } = null!;

        public string fuente_servicio { get; set; } = null!;

        public DateOnly periodo { get; set; }

        public string dimension { get; set; } = null!;

        public decimal? valor { get; set; }

        public DateTimeOffset? actualizado_en { get; set; }

        public DateTimeOffset fecha_creacion { get; set; }

        public DateTimeOffset? fecha_eliminacion { get; set; }

        [ForeignKey(nameof(cliente_ref))]
        [JsonIgnore]
        public ClienteLectura ClienteLecturaReferencia { get; set; } = null!;

        [ForeignKey(nameof(indicador_codigo))]
        [JsonIgnore]
        public Indicador Indicador { get; set; } = null!;
    }
}
