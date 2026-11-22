namespace ComercioInversion.Data;

public sealed record GastoRenta(string Concepto, decimal Importe, string Referencia);
public sealed record TributoRenta(string Impuesto, decimal Importe, DateOnly Fecha, string Referencia);
public sealed record SolicitudRenta(string Periodo, decimal Cobrado, decimal BaseIva, decimal CreditoIva, decimal BaseIt,
    decimal IueAplicado, decimal UtilidadFiscalEstimada, decimal IpbiAnual, decimal Reserva, decimal BobPorXlm,
    GastoRenta[] Gastos, TributoRenta[] PagosTributarios);

public sealed record ContabilidadRenta(SolicitudRenta Registro, long Emitidas, decimal DebitoIva, decimal Iva,
    decimal ItDeterminado, decimal It, decimal ProvisionIue, decimal ProvisionIpbi, decimal TotalGastos,
    decimal Neto, decimal Distribuible)
{
    public static bool Valida(SolicitudRenta r, DateOnly corte) =>
        new[] { r.Cobrado, r.BaseIva, r.CreditoIva, r.BaseIt, r.IueAplicado, r.UtilidadFiscalEstimada, r.IpbiAnual, r.Reserva }
            .All(ImporteValido) &&
        r.BobPorXlm is >= 0.01m and <= 100_000m && ImporteValido(r.BobPorXlm) &&
        r.IueAplicado <= Redondear(r.BaseIt * .03m) &&
        r.Gastos is { Length: <= 60 } && r.Gastos.All(g => g is not null && TextoValido(g.Concepto) &&
            TextoValido(g.Referencia) && g.Importe > 0 && ImporteValido(g.Importe)) &&
        r.PagosTributarios is { Length: <= 24 } && r.PagosTributarios.All(p => p is not null &&
            p.Impuesto is "IVA" or "IT" or "IUE" or "IPBI" && p.Fecha != default && p.Fecha <= corte &&
            TextoValido(p.Referencia) && p.Importe > 0 && ImporteValido(p.Importe));

    public static ContabilidadRenta Calcular(SolicitudRenta r, long emitidas)
    {
        var debito = Redondear(r.BaseIva * .13m);
        var iva = Math.Max(0, debito - r.CreditoIva);
        var itDeterminado = Redondear(r.BaseIt * .03m);
        var it = itDeterminado - r.IueAplicado;
        var iue = Redondear(r.UtilidadFiscalEstimada * .25m);
        var ipbi = Redondear(r.IpbiAnual / 12);
        var gastos = r.Gastos.Sum(g => g.Importe);
        var neto = r.Cobrado - gastos - iva - it - iue - ipbi - r.Reserva;
        return new(r, emitidas, debito, iva, itDeterminado, it, iue, ipbi, gastos, neto, Math.Max(0, neto));
    }

    private static bool TextoValido(string? valor) => !string.IsNullOrWhiteSpace(valor) && valor.Length <= 120;
    private static bool ImporteValido(decimal valor) => valor is >= 0 and <= 100_000_000 && decimal.Round(valor, 2) == valor;
    private static decimal Redondear(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}
