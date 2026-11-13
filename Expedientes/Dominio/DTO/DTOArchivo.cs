using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Expedientes.Dominio.DTO
{
    public class DTOArchivo
    {
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; }
    }
}
