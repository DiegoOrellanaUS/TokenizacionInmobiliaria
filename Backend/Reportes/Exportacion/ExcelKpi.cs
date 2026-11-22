using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace Reportes.Exportacion;

public static class ExcelKpi
{
    private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace P = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Ct = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly XNamespace C = "http://schemas.openxmlformats.org/drawingml/2006/chart";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace X = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";

    public static byte[] Crear(DocumentoKpi informe)
    {
        using var salida = new MemoryStream();
        using (var zip = new ZipArchive(salida, ZipArchiveMode.Create, leaveOpen: true))
        {
            var filas = new List<XElement>();
            var anclas = new List<XElement>();
            var unir = new List<XElement>();
            Fila(filas, 1, informe.Titulo);
            Fila(filas, 0, DatosExportacion.Corte(informe.GeneradoEn));
            Fila(filas, 1, "Indicador", "Valor", "Unidad");
            foreach (var dato in informe.Indicadores) Fila(filas, 0, dato.Etiqueta, dato.Valor, dato.Unidad);

            for (var i = 0; i < informe.Graficas.Count; i++)
            {
                var grafica = informe.Graficas[i];
                Fila(filas, 1, grafica.Titulo);
                Fila(filas, 2, grafica.Explicacion);
                filas[^1].Add(new XAttribute("ht", 32), new XAttribute("customHeight", 1));
                unir.Add(new XElement(S + "mergeCell", new XAttribute("ref", $"A{filas.Count}:C{filas.Count}")));
                Fila(filas, 1, "Categoría", grafica.Unidad);
                var primera = filas.Count + 1;
                foreach (var barra in grafica.Barras) Fila(filas, 0, barra.Etiqueta, barra.Valor);
                Escribir(zip, $"xl/charts/chart{i + 1}.xml", Grafica(grafica, primera));
                anclas.Add(Ancla(i + 1, primera - 3));
                for (var j = grafica.Barras.Count; j < 15; j++) Fila(filas, 0);
            }

            foreach (var tabla in informe.Tablas)
            {
                Fila(filas, 1, tabla.Titulo);
                Fila(filas, 1, tabla.Encabezados);
                foreach (var fila in tabla.Filas) Fila(filas, 0, fila);
                if (tabla.Filas.Length == 0) Fila(filas, 0, "Sin registros en este corte.");
                Fila(filas, 0);
            }

            Escribir(zip, "xl/worksheets/sheet1.xml", new XElement(S + "worksheet",
                new XElement(S + "cols", Col(1, 1, 39), Col(2, 3, 19), Col(4, 4, 64), Col(5, 11, 23)),
                new XElement(S + "sheetData", filas),
                new XElement(S + "mergeCells", new XAttribute("count", unir.Count), unir), new XElement(S + "drawing", new XAttribute(Rel + "id", "rId1"))));
            Escribir(zip, "xl/worksheets/_rels/sheet1.xml.rels", Relaciones(("rId1", "drawing", "../drawings/drawing1.xml")));
            Escribir(zip, "xl/drawings/drawing1.xml", new XElement(X + "wsDr", anclas));
            Escribir(zip, "xl/drawings/_rels/drawing1.xml.rels", Relaciones(Enumerable.Range(1, informe.Graficas.Count)
                .Select(i => ($"rId{i}", "chart", $"../charts/chart{i}.xml")).ToArray()));
            Escribir(zip, "xl/workbook.xml", new XElement(S + "workbook", new XElement(S + "sheets",
                new XElement(S + "sheet", new XAttribute("name", "KPI"), new XAttribute("sheetId", 1), new XAttribute(Rel + "id", "rId1")))));
            Escribir(zip, "xl/_rels/workbook.xml.rels", Relaciones(("rId1", "worksheet", "worksheets/sheet1.xml"),
                ("rId2", "styles", "styles.xml")));
            Escribir(zip, "_rels/.rels", Relaciones(("rId1", "officeDocument", "xl/workbook.xml")));
            Escribir(zip, "xl/styles.xml", XElement.Parse("""
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font></fonts>
                  <fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF214638"/><bgColor indexed="64"/></patternFill></fill></fills>
                  <borders count="1"><border/></borders>
                  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
                  <cellXfs count="3">
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
                    <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/>
                    <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
                  </cellXfs>
                  <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
                </styleSheet>
                """));
            Escribir(zip, "[Content_Types].xml", new XElement(Ct + "Types",
                new XElement(Ct + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(Ct + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                Tipo("/xl/workbook.xml", "spreadsheetml.sheet.main"),
                Tipo("/xl/worksheets/sheet1.xml", "spreadsheetml.worksheet"),
                Tipo("/xl/styles.xml", "spreadsheetml.styles"), Tipo("/xl/drawings/drawing1.xml", "drawing"),
                Enumerable.Range(1, informe.Graficas.Count).Select(i => Tipo($"/xl/charts/chart{i}.xml", "drawingml.chart"))));
        }
        return salida.ToArray();
    }

    private static XElement Grafica(GraficaKpi grafica, int primera)
    {
        var ultima = primera + grafica.Barras.Count - 1;
        var categorias = grafica.Barras.Select(b => b.Etiqueta).ToArray();
        var numeros = grafica.Barras.Select(b => b.Valor.ToString(CultureInfo.InvariantCulture)).ToArray();
        return new XElement(C + "chartSpace", new XElement(C + "chart",
            new XElement(C + "title", new XElement(C + "tx", new XElement(C + "rich",
                new XElement(A + "bodyPr"), new XElement(A + "lstStyle"),
                new XElement(A + "p", new XElement(A + "r", new XElement(A + "t", grafica.Titulo)))))),
            new XElement(C + "plotArea", new XElement(C + "layout"),
                new XElement(C + "barChart", Val("barDir", "bar"), Val("grouping", "clustered"),
                    new XElement(C + "ser", Val("idx", 0), Val("order", 0),
                        new XElement(C + "spPr", new XElement(A + "solidFill",
                            new XElement(A + "srgbClr", new XAttribute("val", "356D59")))),
                        new XElement(C + "cat", Referencia("str", $"KPI!$A${primera}:$A${ultima}", categorias)),
                        new XElement(C + "val", Referencia("num", $"KPI!$B${primera}:$B${ultima}", numeros))),
                    Val("axId", 1), Val("axId", 2)),
                new XElement(C + "catAx", Val("axId", 1), new XElement(C + "scaling", Val("orientation", "maxMin")),
                    Val("axPos", "l"), Val("crossAx", 2), Val("crosses", "autoZero")),
                new XElement(C + "valAx", Val("axId", 2), new XElement(C + "scaling", Val("orientation", "minMax"), Val("min", 0)),
                    Val("axPos", "b"), new XElement(C + "majorGridlines"), Val("crossAx", 1), Val("crosses", "max"))),
            Val("plotVisOnly", 1)));
    }

    private static XElement Referencia(string tipo, string formula, string[] valores) =>
        new(C + (tipo + "Ref"), new XElement(C + "f", formula),
            new XElement(C + (tipo + "Cache"), tipo == "num" ? new XElement(C + "formatCode", "General") : null,
                Val("ptCount", valores.Length), valores.Select((v, i) =>
                    new XElement(C + "pt", new XAttribute("idx", i), new XElement(C + "v", v)))));

    private static XElement Ancla(int id, int fila) => new(X + "twoCellAnchor",
        Marcador("from", 3, fila), Marcador("to", 7, fila + 15),
        new XElement(X + "graphicFrame", new XAttribute("macro", ""),
            new XElement(X + "nvGraphicFramePr",
                new XElement(X + "cNvPr", new XAttribute("id", id), new XAttribute("name", "Gráfica " + id)),
                new XElement(X + "cNvGraphicFramePr")),
            new XElement(X + "xfrm", new XElement(A + "off", new XAttribute("x", 0), new XAttribute("y", 0)),
                new XElement(A + "ext", new XAttribute("cx", 0), new XAttribute("cy", 0))),
            new XElement(A + "graphic", new XElement(A + "graphicData", new XAttribute("uri", C.NamespaceName),
                new XElement(C + "chart", new XAttribute(Rel + "id", $"rId{id}"))))),
        new XElement(X + "clientData"));

    private static XElement Marcador(string nombre, int columna, int fila) => new(X + nombre,
        new XElement(X + "col", columna), new XElement(X + "colOff", 0),
        new XElement(X + "row", fila), new XElement(X + "rowOff", 0));

    private static XElement Val(string nombre, object valor) => new(C + nombre, new XAttribute("val", valor));

    private static void Fila(List<XElement> filas, int estilo, params object?[] valores)
    {
        var numero = filas.Count + 1;
        filas.Add(new XElement(S + "row", new XAttribute("r", numero), valores.Select((valor, i) =>
            new XElement(S + "c", new XAttribute("r", $"{(char)('A' + i)}{numero}"), new XAttribute("s", estilo),
                valor is decimal n ? new XElement(S + "v", n) :
                    new object[] { new XAttribute("t", "inlineStr"), new XElement(S + "is",
                        new XElement(S + "t", Limpio(DatosExportacion.Formato(valor)))) }))));
    }

    private static string Limpio(string entrada) => string.Concat(entrada.EnumerateRunes()
        .Where(r => r.Value is 9 or 10 or 13 || r.Value is >= 32 and not (0xFFFE or 0xFFFF)).Select(r => r.ToString()));

    private static XElement Col(int min, int max, int ancho) => new(S + "col", new XAttribute("min", min),
        new XAttribute("max", max), new XAttribute("width", ancho), new XAttribute("customWidth", 1));

    private static XElement Relaciones(params (string Id, string Tipo, string Destino)[] relaciones) =>
        new(P + "Relationships", relaciones.Select(r => new XElement(P + "Relationship", new XAttribute("Id", r.Id),
            new XAttribute("Type", Rel.NamespaceName + "/" + r.Tipo), new XAttribute("Target", r.Destino))));

    private static XElement Tipo(string parte, string tipo) => new(Ct + "Override", new XAttribute("PartName", parte),
        new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument." + tipo + "+xml"));

    private static void Escribir(ZipArchive zip, string ruta, XElement xml)
    {
        using var stream = zip.CreateEntry(ruta).Open();
        new XDocument(xml).Save(stream, SaveOptions.DisableFormatting);
    }
}
