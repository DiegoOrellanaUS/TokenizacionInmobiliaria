namespace Adminstracion.Dominio;

public class Plan
{
    public Guid id { get; set; }
    public required string codigo { get; set; }
    public required string nombre { get; set; }
    public string? descripcion { get; set; }
    public int revision { get; set; } = 1;
    public decimal precio { get; set; }
    public required string moneda { get; set; }
    public required string periodicidad { get; set; }
    public int max_tokens_propios { get; set; }
    public int max_tokens_como_cliente { get; set; }
    public Guid? plan_origen_id { get; set; }
    public Guid? plan_destino_id { get; set; }
    public DateTimeOffset? oferta_desde { get; set; }
    public DateTimeOffset? oferta_hasta { get; set; }
    public string estado { get; set; } = "ACTIVO";

    public Plan? PlanOrigen { get; set; }
    public Plan? PlanDestino { get; set; }
}
