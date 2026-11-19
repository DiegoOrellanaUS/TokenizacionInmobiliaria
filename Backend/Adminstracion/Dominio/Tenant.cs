namespace Adminstracion.Dominio;

public class Tenant
{
    public Guid id { get; set; }
    public Guid propietario_id { get; set; }
    public string? nombre_comercial { get; set; }
    public string estado { get; set; } = "APROVISIONANDO";

    public Usuario Propietario { get; set; } = null!;
}
