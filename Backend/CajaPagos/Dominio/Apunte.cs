using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CajaPagos.Dominio
{
    [Table("apunte", Schema = "caja")]
    [PrimaryKey(nameof(asiento_id), nameof(linea))]
    public class Apunte
    {
        public Guid asiento_id { get; set; }

        public int linea { get; set; }

        public Guid cuenta_id { get; set; }

        public string lado { get; set; } = null!;

        public decimal importe { get; set; }

        [ForeignKey(nameof(asiento_id))]
        [JsonIgnore]
        public Asiento Asiento { get; set; } = null!;

        [ForeignKey(nameof(cuenta_id))]
        [JsonIgnore]
        public Cuenta Cuenta { get; set; } = null!;
    }
}
