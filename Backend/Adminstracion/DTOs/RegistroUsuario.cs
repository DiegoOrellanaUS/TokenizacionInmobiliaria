using System.ComponentModel.DataAnnotations;

namespace Adminstracion.DTOs
{
    public class RegistroUsuario
    {
        [Required]
        public string CI { get; set; } = null!;

        [Required]
        public string Nombre { get; set; } = null!;

        public string? Apellidos { get; set; }

        [Required, EmailAddress]
        public string CorreoElectronico { get; set; } = null!;

        [Required, StringLength(128)]
        public string Contrasenia { get; set; } = null!;

        [StringLength(128)]
        public string? Invitacion { get; set; }
    }
}
