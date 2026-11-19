using System.Text.Json.Serialization;

namespace Adminstracion.Dominio;

public class UsuarioRol
{
    public Guid id { get; set; }
    public Guid usuario_id { get; set; }
    public Guid rol_id { get; set; }
    public required string alcance { get; set; }
    public Guid? tenant_id { get; set; }
    public Guid? token_ref { get; set; }
    [JsonIgnore]
    public string? silos_por_servicio { get; set; }
    public DateTimeOffset fecha_creacion { get; set; }
    public DateTimeOffset? fecha_eliminacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
    public Tenant? Tenant { get; set; }
}
