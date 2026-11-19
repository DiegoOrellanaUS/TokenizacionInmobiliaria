namespace Adminstracion.Dominio;

public class Suscripcion
{
    public Guid id { get; set; }
    public Guid usuario_id { get; set; }
    public Guid tenant_id { get; set; }
    public Guid plan_id { get; set; }
    public required string condiciones_contratadas { get; set; }
    public string? proveedor_pago { get; set; }
    public string? referencia_pago { get; set; }
    public decimal? importe_pagado { get; set; }
    public DateTimeOffset? pago_confirmado_en { get; set; }
    public DateTimeOffset desde { get; set; }
    public DateTimeOffset? hasta { get; set; }
    public string estado { get; set; } = "PENDIENTE";

    public Usuario Usuario { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
}
