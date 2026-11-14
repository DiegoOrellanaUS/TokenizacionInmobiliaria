using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Expedientes.Dominio
{
    public class EmpleadoArchivo
    {
        [Key]
        public int EmpleadoArchivoId { get; set; }
        [ForeignKey(nameof(Empleado))]
        public int EmpleadoId { get; set; }
        [JsonIgnore]
        public Empleado Empleado { get; set; }
        public string CodigoArchivo { get; set; }
        public string Estado { get; set; }
    }
}
