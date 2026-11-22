using System.Globalization;
using Reportes.Exportacion;
using static Global.Servicios.ConsultarGlobal;

namespace Global.Servicios;

public sealed record InformePanelGlobal(DateTimeOffset GeneradoEn, bool Listo, FuenteGlobal[] Fuentes,
    IndicadorKpi[] Indicadores, GraficaKpi[] Graficas, ImporteGlobal[] Importes, MesGlobal[] Meses)
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-BO");
    private static readonly TimeZoneInfo Bolivia = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");

    public static InformePanelGlobal Crear(InformeGlobal datos) => new(datos.GeneradoEn, datos.CargaInicialCompleta, datos.Fuentes,
        [new("Espacios activos", datos.TenantsActivos, "espacios"), new("Inmuebles registrados", datos.InmueblesRegistrados, "inmuebles"),
         new("Ofertas registradas", datos.OfertasRegistradas, "ofertas"), new("Tokens vendidos", datos.TokensVendidos, "tokens"),
         new("Colocación de tokens", datos.PorcentajeColocado, "%"), new("Compras confirmadas", datos.ComprasConfirmadas, "compras")],
        [new("Espacios registrados y activos", "espacios",
             $"{N(datos.TenantsActivos)} de {N(datos.TenantsRegistrados)} espacios tienen una suscripción activa y están habilitados para publicar.",
             [new("Registrados", datos.TenantsRegistrados), new("Activos", datos.TenantsActivos)]),
         new("Estado de los inmuebles", "inmuebles",
             $"Hay {N(datos.InmueblesRegistrados)} inmuebles registrados. Pendientes de una decisión de revisión: {N(datos.InmueblesEnRevision)}.",
             [new("Borrador", datos.InmueblesBorrador), new("En revisión", datos.InmueblesEnRevision),
              new("Observados", datos.InmueblesObservados), new("Aprobados", datos.InmueblesAprobados)]),
         new("Emisión y colocación de tokens", "tokens", datos.PorcentajeColocado is { } porcentaje
             ? $"Las compras confirmadas representan {N(datos.TokensVendidos)} de {N(datos.TokensOfrecidos)} tokens ofrecidos ({N(porcentaje)} %)."
             : "Hay datos de emisión y el porcentaje de colocación se calcula cuando existen tokens ofrecidos.",
             [new("Emitidos", datos.TokensEmitidos), new("Ofrecidos", datos.TokensOfrecidos), new("Vendidos", datos.TokensVendidos)]),
         new("Estado de las compras", "compras",
             $"Compras confirmadas: {N(datos.ComprasConfirmadas)}. Canceladas: {N(datos.ComprasCanceladas)}. Los importes se calculan con compras confirmadas de prueba.",
             [new("Pendientes", datos.ComprasPendientes), new("Confirmadas", datos.ComprasConfirmadas),
              new("Fallidas", datos.ComprasFallidas), new("Canceladas", datos.ComprasCanceladas)])],
        datos.ImportesConfirmados, datos.ComprasMensuales);

    public DocumentoKpi Exportar() => new("Umbral | Indicadores globales", GeneradoEn, Indicadores, Graficas,
        [new("Importes confirmados por moneda", ["Moneda", "Importe"], [195, 316],
            Importes.Select(i => new object?[] { i.Moneda, i.Importe }).ToArray()),
         new("Compras confirmadas por mes de registro", ["Mes", "Moneda", "Compras", "Importe"], [120, 100, 121, 170],
            Meses.Select(m => new object?[] { m.MesRegistro, m.Moneda, m.ComprasConfirmadas, m.Importe }).ToArray()),
         new("Corte por fuente (Bolivia)", ["Fuente", "Última sincronización"], [200, 311],
            Fuentes.Select(f => new object?[] { NombreFuente(f.Fuente), f.ActualizadoEn is { } fecha
                ? TimeZoneInfo.ConvertTime(fecha, Bolivia).ToString("dd/MM/yyyy HH:mm", Cultura) : "En espera" }).ToArray())]);

    private static string NombreFuente(string fuente) => fuente switch
    {
        "admin" => "Administración", "inmuebles" => "Inmuebles", "tokenizacion" => "Tokenización",
        "comercio" => "Comercio", _ => fuente
    };

    private static string N(decimal valor) => valor.ToString("#,0.#", Cultura);
}
