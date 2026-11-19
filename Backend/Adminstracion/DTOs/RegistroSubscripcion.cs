using System.ComponentModel.DataAnnotations;

namespace Adminstracion.DTOs
{
    public class RegistroSubscripcion
    {
        [Required]
        public string CodigoPlan { get; set; } = null!;

        public Guid UsuarioId { get; set; }

        [Required]
        public string ReferenciaPago { get; set; } = null!;
    }
}
