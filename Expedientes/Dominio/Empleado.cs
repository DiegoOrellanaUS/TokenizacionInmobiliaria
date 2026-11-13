using System.ComponentModel.DataAnnotations;

namespace Expedientes.Dominio
{
    public class Empleado
    {
        [Key]
        public int EmpleadoId { get; set; }
        public string Nombre { get; set; }
        public string CI { get; set; }
        public string Estado { get; set; }
    }
}
