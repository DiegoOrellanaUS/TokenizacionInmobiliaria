using System.ComponentModel.DataAnnotations;

namespace Archivos.Dominio
{
    public class Archivo
    {
        [Key]
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Ruta { get; set; }
        public string Tipo { get; set; }
        public string Contenido { get; set; }
        public string Estado { get; set; }
    }
}
