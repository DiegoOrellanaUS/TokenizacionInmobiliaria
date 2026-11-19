namespace Adminstracion.Dominio;

public class Rol
{
    public Guid id { get; set; }
    public required string codigo { get; set; }
    public required string nombre_visible { get; set; }
    public string? descripcion { get; set; }
    public int? orden_visual { get; set; }
    public string estado { get; set; } = "ACTIVO";
}
