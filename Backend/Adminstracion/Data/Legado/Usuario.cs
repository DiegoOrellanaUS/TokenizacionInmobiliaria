using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Adminstracion.Data.Legado
{
    [Index(nameof(TenantId), IsUnique = true)]
    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }
        public string CI { get; set; }
        public string Nombre { get; set; }
        public string CorreoElectronico { get; set; }
        public string Contrasenia { get; set; }
        public string Estado { get; set; }
        public Guid TenantId { get; set; }
        public string EstadoBaseDatos { get; set; } = "Pendiente";
        public List<Subscripcion> Subscripciones { get; set; }
    }
}
