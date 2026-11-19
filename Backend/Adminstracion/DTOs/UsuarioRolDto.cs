namespace Adminstracion.DTOs;

public class AsignarRol
{
    public Guid RolId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? TokenRef { get; set; }
}

public record UsuarioRolDetalle(
    Guid Id, Guid UsuarioId, Guid RolId, string CodigoRol, string EstadoRol,
    string Alcance, Guid? TenantId, Guid? TokenRef,
    DateTimeOffset FechaCreacion, DateTimeOffset? FechaEliminacion);
