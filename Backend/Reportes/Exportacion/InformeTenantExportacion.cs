using Reportes.Servicios;

namespace Reportes.Exportacion;

internal static class InformeTenantExportacion
{
    public static DocumentoKpi Crear(InformeKpi informe) => new("Umbral | Indicadores del tenant",
        informe.GeneradoEn, informe.Indicadores, informe.Graficas,
        [
            new("Ventas confirmadas por moneda", ["Moneda", "Unidades", "Importe", "Precio medio pagado"],
                [100, 110, 150, 151], informe.Ventas.Select(v => new object?[]
                    { v.Moneda, v.Unidades, v.Importe, v.PrecioMedio }).ToArray()),
            new("Compras confirmadas por mes de registro", ["Mes", "Moneda", "Compras", "Importe"],
                [120, 100, 121, 170], informe.Meses.Select(m => new object?[]
                    { m.Mes, m.Moneda, m.Compras, m.Importe }).ToArray()),
            new("Procedencia y precio de los tokens", ["Inmueble", "Activo", "Red", "Cuenta emisora", "Moneda",
                "Emitidos", "Ofrecidos", "Vendidos", "Precio publicado", "Importe confirmado", "Precio medio pagado"],
                [], informe.Ofertas.Select(o => new object?[] { o.Inmueble, o.Codigo, o.Red, o.Emisor, o.Moneda, o.Emitidas,
                    o.Ofrecidas, o.Vendidas, o.PrecioPublicado, o.Importe, o.PrecioMedio }).ToArray())
        ],
        new("Procedencia y precio de los tokens", ["Inmueble / activo", "Unidades", "Importes"],
            [205, 153, 153], informe.Ofertas.Select(o => new object?[]
            {
                $"{o.Inmueble}\n{o.Codigo} · {o.Red ?? "Red sin sincronizar"}\n{o.Emisor ?? "Emisor sin sincronizar"}",
                $"Emitidos {DatosExportacion.Formato(o.Emitidas)}\nOfrecidos {DatosExportacion.Formato(o.Ofrecidas)}\nVendidos {DatosExportacion.Formato(o.Vendidas)}",
                $"{o.Moneda} · Precio publicado {DatosExportacion.Formato(o.PrecioPublicado)}\nConfirmado {DatosExportacion.Formato(o.Importe)}\nPrecio medio {DatosExportacion.Formato(o.PrecioMedio)}"
            }).ToArray()));
}
