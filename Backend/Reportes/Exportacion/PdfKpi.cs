using System.Globalization;
using System.Text;

namespace Reportes.Exportacion;

public static class PdfKpi
{
    public static byte[] Crear(DocumentoKpi informe)
    {
        var pdf = new InformePdf(informe.Titulo, informe.GeneradoEn);
        pdf.Tabla("Indicadores", ["Indicador", "Valor", "Unidad"], [290, 104, 117],
            informe.Indicadores.Select(i => new object?[] { i.Etiqueta, i.Valor, i.Unidad }));
        foreach (var grafica in informe.Graficas)
            pdf.Grafica(grafica);
        foreach (var tabla in informe.Tablas.Where(t => t.AnchosPdf.Length > 0))
            pdf.Tabla(tabla.Titulo, tabla.Encabezados, tabla.AnchosPdf, tabla.Filas);
        if (informe.AnexoPdf is { } anexo)
            pdf.Tabla(anexo.Titulo, anexo.Encabezados, anexo.AnchosPdf, anexo.Filas);
        return pdf.Terminar();
    }

    private sealed class InformePdf(string titulo, DateTimeOffset corte)
    {
        private readonly List<StringBuilder> paginas = [];
        private StringBuilder pagina = new();
        private int y;

        public void Tabla(string nombre, string[] titulos, int[] anchos, IEnumerable<object?[]> filas)
        {
            using var registros = filas.GetEnumerator();
            var tieneFilas = registros.MoveNext();
            var lineas = tieneFilas ? Lineas(registros.Current, anchos) : null;
            Asegurar(52 + (lineas is null ? 28 : Altura(lineas)));
            Titulo(nombre);
            Fila(titulos, anchos, true);
            var alterna = false;
            while (lineas is not null)
            {
                var altura = Altura(lineas);
                if (y - altura < 44)
                {
                    NuevaPagina();
                    Titulo(nombre + " (continuación)");
                    Fila(titulos, anchos, true);
                }
                Fila(lineas, anchos, altura, false, alterna);
                alterna = !alterna;
                lineas = registros.MoveNext() ? Lineas(registros.Current, anchos) : null;
            }
            if (!tieneFilas)
            {
                Texto(48, y - 16, 10, "Sin registros en este corte.");
                y -= 28;
            }
            y -= 12;
        }

        public void Grafica(GraficaKpi grafica)
        {
            var lineas = Envolver(grafica.Explicacion, 88);
            Asegurar(48 + grafica.Barras.Count * 19 + lineas.Length * 13);
            Titulo(grafica.Titulo + " (" + grafica.Unidad + ")");
            var maximo = Math.Max(1, grafica.Barras.Max(b => b.Valor));
            foreach (var barra in grafica.Barras)
            {
                Texto(46, y - 11, 9, barra.Etiqueta);
                Rectangulo(156, y - 14, 250, 12, "0.91 0.94 0.92");
                if (barra.Valor > 0)
                    Rectangulo(156, y - 14, Math.Max(1, (int)(250 * barra.Valor / maximo)), 12, "0.18 0.43 0.34");
                Texto(418, y - 11, 9, DatosExportacion.Formato(barra.Valor), true);
                y -= 19;
            }
            y -= 1;
            foreach (var linea in lineas)
            {
                Texto(46, y - 8, 9, linea);
                y -= 13;
            }
            y -= 12;
        }

        private static string[][] Lineas(object?[] fila, int[] anchos) =>
            fila.Select((celda, i) => Envolver(DatosExportacion.Formato(celda), (anchos[i] - 14) / 5)).ToArray();

        private static int Altura(string[][] lineas) => 12 * lineas.Max(c => c.Length) + 10;

        private void Fila(string[] celdas, int[] anchos, bool encabezado) =>
            Fila(celdas.Select(c => new[] { c }).ToArray(), anchos, 23, encabezado, false);

        private void Fila(string[][] celdas, int[] anchos, int alto, bool encabezado, bool alterna)
        {
            if (encabezado || alterna)
                Rectangulo(42, y - alto, 511, alto, encabezado ? "0.13 0.27 0.22" : "0.94 0.96 0.95");
            var x = 42;
            for (var i = 0; i < celdas.Length; i++)
            {
                for (var j = 0; j < celdas[i].Length; j++)
                    Texto(x + 7, y - 14 - j * 12, 9, celdas[i][j], encabezado, encabezado);
                x += anchos[i];
            }
            y -= alto;
        }

        private void Titulo(string titulo)
        {
            Texto(42, y - 12, 13, titulo, true);
            y -= 23;
        }

        private void Asegurar(int espacio)
        {
            if (paginas.Count == 0 || y - espacio < 44) NuevaPagina();
        }

        private void NuevaPagina()
        {
            pagina = new StringBuilder();
            paginas.Add(pagina);
            Rectangulo(0, 743, 595, 99, "0.13 0.27 0.22");
            Texto(42, 790, 21, titulo, true, true);
            Texto(42, 766, 10, DatosExportacion.Corte(corte), blanco: true);
            Texto(42, 27, 9, $"Umbral · Página {paginas.Count}");
            y = 723;
        }

        private void Rectangulo(int x, int bajo, int ancho, int alto, string rgb) =>
            pagina.AppendLine(CultureInfo.InvariantCulture, $"{rgb} rg {x} {bajo} {ancho} {alto} re f");

        private void Texto(int x, int bajo, int tamano, string texto, bool negrita = false, bool blanco = false)
        {
            // Las fuentes PDF estándar con WinAnsi cubren los caracteres usados en el informe en español.
            var literal = Convert.ToHexString(Encoding.Latin1.GetBytes(texto));
            pagina.AppendLine(CultureInfo.InvariantCulture,
                $"{(blanco ? "1 1 1" : "0.12 0.19 0.16")} rg BT /F{(negrita ? 2 : 1)} {tamano} Tf {x} {bajo} Td <{literal}> Tj ET");
        }

        private static string[] Envolver(string texto, int ancho)
        {
            var salida = new List<string>();
            foreach (var parrafo in texto.Replace("\r", "").Split('\n'))
            {
                var resto = parrafo.Trim();
                while (resto.Length > ancho)
                {
                    var corte = resto.LastIndexOf(' ', ancho);
                    if (corte < 1) corte = ancho;
                    salida.Add(resto[..corte]);
                    resto = resto[corte..].TrimStart();
                }
                salida.Add(resto);
            }
            return [.. salida];
        }

        public byte[] Terminar()
        {
            using var stream = new MemoryStream();
            var posiciones = new List<long> { 0 };
            void Escribir(string texto) => stream.Write(Encoding.ASCII.GetBytes(texto));
            void Objeto(string contenido)
            {
                posiciones.Add(stream.Position);
                Escribir($"{posiciones.Count - 1} 0 obj\n{contenido}\nendobj\n");
            }

            Escribir("%PDF-1.4\n");
            Objeto("<< /Type /Catalog /Pages 2 0 R >>");
            Objeto($"<< /Type /Pages /Count {paginas.Count} /Kids [{string.Join(" ", Enumerable.Range(0, paginas.Count).Select(i => $"{5 + 2 * i} 0 R"))}] >>");
            Objeto("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
            Objeto("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
            foreach (var contenido in paginas)
            {
                var paginaId = posiciones.Count;
                Objeto($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {paginaId + 1} 0 R >>");
                var dibujo = contenido.ToString();
                Objeto($"<< /Length {dibujo.Length} >>\nstream\n{dibujo}endstream");
            }
            var xref = stream.Position;
            Escribir($"xref\n0 {posiciones.Count}\n0000000000 65535 f \n");
            foreach (var posicion in posiciones.Skip(1))
                Escribir(posicion.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
            Escribir($"trailer\n<< /Size {posiciones.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            return stream.ToArray();
        }
    }
}
