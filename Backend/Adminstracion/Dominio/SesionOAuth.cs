namespace Adminstracion.Dominio;

public sealed class SesionOAuth
{
    public required string TokenHash { get; set; }
    public required string AccessHash { get; set; }
    public DateTimeOffset VenceAccess { get; set; }
    public Guid FamiliaId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTimeOffset VenceEn { get; set; }
    public DateTimeOffset? UsadoEn { get; set; }
    public DateTimeOffset? RevocadoEn { get; set; }
}
