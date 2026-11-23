namespace Adminstracion.Dominio;

public sealed class VinculoOAuth
{
    public required string Emisor { get; init; }
    public required string Sujeto { get; init; }
    public Guid UsuarioId { get; init; }
}
