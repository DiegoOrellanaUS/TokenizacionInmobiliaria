using System.Text.Json.Serialization;

namespace Adminstracion.Dominio;

public class Usuario
{
    public Guid id { get; set; }
    public required string nombres { get; set; }
    public required string apellidos { get; set; }
    public string? ci { get; set; }
    public string? correo_electronico { get; set; }
    public string? emisor_oidc { get; set; }
    public string? sujeto_oidc { get; set; }
    [JsonIgnore]
    public string? contrasenia_hash { get; set; }
    public string estado { get; set; } = "ACTIVO";
}
