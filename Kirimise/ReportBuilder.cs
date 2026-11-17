using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace Kirimise;

public enum Periodo { Dia, Mes, Anio }

public static class ReportBuilder
{
    const string Rojo = "#C8283C", Tinta = "#2A1B2E", Gris = "#7A6A70", Linea = "#E6DCD2", Encabezado = "#F4EDE4";

    record Renglon(string Nombre, int Cant, decimal Importe);

    static IEnumerable<Renglon> Renglones(Venta v, Dictionary<string, decimal> precios)
    {
        if (v.Lineas.Count > 0) return v.Lineas.Select(l => new Renglon(l.Nombre, l.Cant, l.Importe));
        var lista = new List<Renglon>();
        foreach (var parte in v.Resumen.Split(", ", StringSplitOptions.RemoveEmptyEntries))
        {
            var m = Regex.Match(parte, @"^(\d+)x (.+)$");
            if (!m.Success) continue;
            int cant = int.Parse(m.Groups[1].Value); var nombre = m.Groups[2].Value;
            lista.Add(new Renglon(nombre, cant, cant * (precios.TryGetValue(nombre, out var p) ? p : 0m)));
        }
        return lista;
    }

    static IContainer Th(IContainer c) => c.Background(Encabezado).PaddingVertical(5).PaddingHorizontal(6).DefaultTextStyle(x => x.SemiBold().FontSize(9));
    static IContainer Td(IContainer c) => c.BorderBottom(1).BorderColor(Linea).PaddingVertical(4).PaddingHorizontal(6);

    public static void Build(string path, Periodo tipo, string etiqueta, List<Venta> ventas, string logoPath, string admin, Dictionary<string, decimal> precios)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var mx = MenuData.MX;
        var total = ventas.Sum(v => v.Total);
        var promedio = ventas.Count > 0 ? total / ventas.Count : 0m;
        var renglones = ventas.SelectMany(v => Renglones(v, precios)).ToList();
        var unidades = renglones.Sum(r => r.Cant);
        var topProductos = renglones.GroupBy(r => r.Nombre)
            .Select(g => new Renglon(g.Key, g.Sum(x => x.Cant), g.Sum(x => x.Importe)))
            .OrderByDescending(r => r.Cant).ThenByDescending(r => r.Importe).ToList();
        var tipoTxt = tipo switch { Periodo.Dia => "Día", Periodo.Mes => "Mes", _ => "Año" };

        var desglose = tipo switch
        {
            Periodo.Mes => ventas.GroupBy(v => v.Fecha.Date).OrderBy(g => g.Key)
                .Select(g => (g.Key.ToString("dddd dd/MM", mx), g.Count(), g.Sum(v => v.Total))).ToList(),
            Periodo.Anio => ventas.GroupBy(v => v.Fecha.Month).OrderBy(g => g.Key)
                .Select(g => (char.ToUpper(mx.DateTimeFormat.GetMonthName(g.Key)[0]) + mx.DateTimeFormat.GetMonthName(g.Key)[1..], g.Count(), g.Sum(v => v.Total))).ToList(),
            _ => new List<(string, int, decimal)>()
        };

        Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor(Tinta));

            page.Header().PaddingBottom(10).BorderBottom(1.5f).BorderColor(Rojo).Row(row =>
            {
                if (File.Exists(logoPath)) row.ConstantItem(64).Image(File.ReadAllBytes(logoPath));
                row.RelativeItem().PaddingLeft(12).Column(c =>
                {
                    c.Item().Text("Kirimise").FontSize(24).Bold().FontColor(Rojo);
                    c.Item().Text("Reporte de ventas").FontSize(13).SemiBold();
                    c.Item().Text($"{tipoTxt}: {etiqueta}").FontColor(Gris);
                });
                row.ConstantItem(170).AlignRight().Column(c =>
                {
                    c.Item().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Gris);
                    c.Item().Text($"Por {admin}").FontSize(9).FontColor(Gris);
                });
            });

            page.Content().PaddingVertical(14).Column(col =>
            {
                col.Spacing(16);

                col.Item().Row(r =>
                {
                    r.Spacing(8);
                    void Tarjeta(string titulo, string valor) =>
                        r.RelativeItem().Border(1).BorderColor(Linea).Padding(10).Column(c =>
                        {
                            c.Item().Text(titulo).FontSize(8).SemiBold().FontColor(Gris);
                            c.Item().Text(valor).FontSize(16).Bold();
                        });
                    Tarjeta("TOTAL VENDIDO", total.ToString("C2", mx));
                    Tarjeta("VENTAS", ventas.Count.ToString());
                    Tarjeta("TICKET PROMEDIO", promedio.ToString("C2", mx));
                    Tarjeta("PRODUCTOS VENDIDOS", unidades.ToString());
                });

                if (desglose.Count > 0)
                    col.Item().Column(c =>
                    {
                        c.Item().PaddingBottom(4).Text(tipo == Periodo.Mes ? "Ventas por día" : "Ventas por mes").FontSize(12).Bold();
                        c.Item().Table(t =>
                        {
                            t.ColumnsDefinition(d => { d.RelativeColumn(); d.ConstantColumn(70); d.ConstantColumn(100); });
                            t.Header(h =>
                            {
                                h.Cell().Element(Th).Text(tipo == Periodo.Mes ? "Día" : "Mes");
                                h.Cell().Element(Th).AlignRight().Text("Ventas");
                                h.Cell().Element(Th).AlignRight().Text("Total");
                            });
                            foreach (var (nombre, n, suma) in desglose)
                            {
                                t.Cell().Element(Td).Text(nombre);
                                t.Cell().Element(Td).AlignRight().Text(n.ToString());
                                t.Cell().Element(Td).AlignRight().Text(suma.ToString("C2", mx));
                            }
                        });
                    });

                if (topProductos.Count > 0)
                    col.Item().Column(c =>
                    {
                        c.Item().PaddingBottom(4).Text("Productos vendidos").FontSize(12).Bold();
                        c.Item().Table(t =>
                        {
                            t.ColumnsDefinition(d => { d.RelativeColumn(); d.ConstantColumn(70); d.ConstantColumn(100); });
                            t.Header(h =>
                            {
                                h.Cell().Element(Th).Text("Producto");
                                h.Cell().Element(Th).AlignRight().Text("Unidades");
                                h.Cell().Element(Th).AlignRight().Text("Importe");
                            });
                            foreach (var p in topProductos)
                            {
                                t.Cell().Element(Td).Text(p.Nombre);
                                t.Cell().Element(Td).AlignRight().Text(p.Cant.ToString());
                                t.Cell().Element(Td).AlignRight().Text(p.Importe.ToString("C2", mx));
                            }
                        });
                    });

                if (tipo != Periodo.Anio && ventas.Count > 0)
                    col.Item().Column(c =>
                    {
                        c.Item().PaddingBottom(4).Text("Detalle de ventas").FontSize(12).Bold();
                        c.Item().Table(t =>
                        {
                            t.ColumnsDefinition(d => { d.ConstantColumn(54); d.ConstantColumn(78); d.ConstantColumn(80); d.RelativeColumn(); d.ConstantColumn(72); });
                            t.Header(h =>
                            {
                                h.Cell().Element(Th).Text("Folio");
                                h.Cell().Element(Th).Text("Fecha");
                                h.Cell().Element(Th).Text("Cliente");
                                h.Cell().Element(Th).Text("Consumo");
                                h.Cell().Element(Th).AlignRight().Text("Total");
                            });
                            foreach (var v in ventas.OrderBy(v => v.Fecha))
                            {
                                t.Cell().Element(Td).Text(v.Folio).FontSize(9);
                                t.Cell().Element(Td).Text(v.Fecha.ToString("dd/MM HH:mm")).FontSize(9);
                                t.Cell().Element(Td).Text(v.Cliente).FontSize(9);
                                t.Cell().Element(Td).Text(v.Resumen).FontSize(9);
                                t.Cell().Element(Td).AlignRight().Text(v.Total.ToString("C2", mx)).FontSize(9);
                            }
                        });
                    });
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(8).FontColor(Gris));
                t.Span("Kirimise · Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        })).GeneratePdf(path);
    }
}
