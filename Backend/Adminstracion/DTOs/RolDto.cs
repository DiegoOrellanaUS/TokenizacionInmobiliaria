using System.ComponentModel.DataAnnotations;

namespace Adminstracion.DTOs;

public class EditarRol
{
    [Required]
    public string NombreVisible { get; set; } = null!;

    public string? Descripcion { get; set; }

    [Range(0, int.MaxValue)]
    public int? OrdenVisual { get; set; }

    public string? Estado { get; set; }
}

public class CrearRol
{
    [Required]
    public string Codigo { get; set; } = null!;

    [Required]
    public string NombreVisible { get; set; } = null!;

    public string? Descripcion { get; set; }

    [Range(0, int.MaxValue)]
    public int? OrdenVisual { get; set; }
}
