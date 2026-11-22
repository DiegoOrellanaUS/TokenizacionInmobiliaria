namespace Adminstracion.Dominio;

public sealed class InvitacionRevisionCanjeada
{
    public required string CodigoHash { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTimeOffset CanjeadaEn { get; set; }
}
