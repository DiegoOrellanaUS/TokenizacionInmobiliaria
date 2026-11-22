namespace Reportes.Exportacion;

public sealed record BarraKpi(string Etiqueta, decimal Valor);
public sealed record GraficaKpi(string Titulo, string Unidad, string Explicacion, IReadOnlyList<BarraKpi> Barras);
public sealed record IndicadorKpi(string Etiqueta, decimal? Valor, string Unidad);
public sealed record TablaKpi(string Titulo, string[] Encabezados, int[] AnchosPdf, object?[][] Filas);
public sealed record DocumentoKpi(string Titulo, DateTimeOffset GeneradoEn, IReadOnlyList<IndicadorKpi> Indicadores,
    IReadOnlyList<GraficaKpi> Graficas, IReadOnlyList<TablaKpi> Tablas, TablaKpi? AnexoPdf = null);
