using System.ComponentModel.DataAnnotations;

namespace InmueblesOperacion.DTOs;

public record RegistroInmueble(
    [Required, StringLength(200)] string Nombre,
    [Required, StringLength(40)] string Tipo,
    [Required, StringLength(120)] string Ciudad,
    [Required, StringLength(250)] string Direccion,
    [Range(typeof(decimal), "0.01", "999999999999.99", ParseLimitsInInvariantCulture = true)] decimal SuperficieM2);

public record TitularDeclarado(
    [Required, StringLength(200)] string Nombre, DateOnly Desde,
    [Range(typeof(decimal), "0.00000001", "1", ParseLimitsInInvariantCulture = true)] decimal? Cuota);

public record DeclararTitulares([Required, MinLength(1), MaxLength(20)] TitularDeclarado[] Titulares);

public sealed class AdjuntarDocumento
{
    [Required]
    public string Clase { get; init; } = null!;
    [Required]
    public IFormFile Archivo { get; init; } = null!;
}

public sealed class ReemplazarDocumento
{
    [Required]
    public IFormFile Archivo { get; init; } = null!;
}

public record ResolverRevision(
    [Range(typeof(long), "1", "9223372036854775807", ParseLimitsInInvariantCulture = true)] long EnvioId,
    [Required, RegularExpression("APROBADO|CON_OBSERVACIONES")] string Decision,
    [StringLength(2000)] string? Observacion);

public record InmuebleDetalle(Guid Id, string Nombre, string? Tipo, string? Ciudad, string? Direccion,
    decimal SuperficieM2, string EstadoRevision);
