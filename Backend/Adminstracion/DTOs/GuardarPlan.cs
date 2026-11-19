using System.ComponentModel.DataAnnotations;

namespace Adminstracion.DTOs;

public abstract class DatosPlan
{
    [Required]
    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }

    [Required, MaxLength(12)]
    public string Moneda { get; set; } = null!;

    [Required]
    public string Periodicidad { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int MaxTokensPropios { get; set; }

    [Range(0, int.MaxValue)]
    public int MaxTokensComoCliente { get; set; }
}

public class CrearPlan : DatosPlan
{
    [Required]
    public string Codigo { get; set; } = null!;
}

public class EditarPlan : DatosPlan
{
    public string? Estado { get; set; }
}

public class CrearOfertaPlan : CrearPlan
{
    [Required]
    public DatosOfertaPlan Oferta { get; set; } = null!;
}

public class EditarOfertaPlan : EditarPlan
{
    [Required]
    public DatosOfertaPlan Oferta { get; set; } = null!;
}

public class DatosOfertaPlan
{
    public Guid? PlanOrigenId { get; set; }
    public Guid PlanDestinoId { get; set; }
    public DateTimeOffset? OfertaDesde { get; set; }
    public DateTimeOffset? OfertaHasta { get; set; }
}
