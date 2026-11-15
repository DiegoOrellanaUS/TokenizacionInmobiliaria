using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Reportes.Dominio
{
    [Table("evento_recibido", Schema = "reportes")]
    [PrimaryKey(nameof(fuente), nameof(evento_ref))]
    public class EventoRecibido
    {
        public string fuente { get; set; } = null!;

        public Guid evento_ref { get; set; }

        public DateTimeOffset? procesado_en { get; set; }
    }
}
