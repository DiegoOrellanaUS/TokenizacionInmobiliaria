using System.Globalization;

namespace Reportes.Exportacion;

internal static class DatosExportacion
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-BO");
    private static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");

    public static string Corte(DateTimeOffset fecha) =>
        "Consultado el " + TimeZoneInfo.ConvertTime(fecha, Zona).ToString("dd/MM/yyyy HH:mm", Cultura) + " (Bolivia)";

    public static string Formato(object? valor) => valor switch
    {
        null => "Sin datos",
        decimal numero => numero.ToString("#,0.##", Cultura),
        _ => (string)valor
    };
}
