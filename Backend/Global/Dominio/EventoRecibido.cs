using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Global.Dominio
{
    [Table("evento_recibido", Schema = "global")]
    [PrimaryKey(nameof(cliente_ref), nameof(evento_ref))]
    public class EventoRecibido
    {
        public Guid cliente_ref { get; set; }

        public Guid evento_ref { get; set; }

        public DateTimeOffset? procesado_en { get; set; }

        [ForeignKey(nameof(cliente_ref))]
        [JsonIgnore]
        public ClienteLectura ClienteLecturaReferencia { get; set; } = null!;
    }
}
