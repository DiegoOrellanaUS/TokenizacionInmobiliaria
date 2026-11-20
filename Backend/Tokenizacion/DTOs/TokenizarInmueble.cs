using System.ComponentModel.DataAnnotations;

namespace Tokenizacion.DTOs;

public record TokenizarInmueble(
    [Required, RegularExpression("[A-Z0-9]{1,12}")] string Codigo,
    [Range(typeof(long), "1", "922337203685", ParseLimitsInInvariantCulture = true)] long Unidades);

public record ResultadoEmision(Guid InmuebleId, Guid ActivoId, string Codigo, long Unidades,
    string Red, string Emisor, string Distribuidora, string Estado, string? Hash, long? Ledger);
