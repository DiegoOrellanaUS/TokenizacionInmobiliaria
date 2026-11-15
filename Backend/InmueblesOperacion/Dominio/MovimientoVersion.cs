using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InmueblesOperacion.Dominio
{
    [Table("movimiento_version", Schema = "operacion")]
    [PrimaryKey(nameof(movimiento_id), nameof(version))]
    public class MovimientoVersion
    {
        public Guid movimiento_id { get; set; }

        public int version { get; set; }

        public Guid categoria_id { get; set; }

        public DateOnly fecha { get; set; }

        public decimal importe { get; set; }

        [MaxLength(12)]
        public string moneda { get; set; } = null!;

        public string? referencia { get; set; }

        [ForeignKey(nameof(movimiento_id))]
        [JsonIgnore]
        public Movimiento Movimiento { get; set; } = null!;

        [ForeignKey(nameof(categoria_id))]
        [JsonIgnore]
        public Categoria Categoria { get; set; } = null!;
    }
}
