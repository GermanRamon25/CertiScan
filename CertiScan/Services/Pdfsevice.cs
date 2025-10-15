using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using QuestPDF.Drawing;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Linq;

namespace CertiScan.Services
{
    public class PdfService
    {
        static PdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public string ExtraerTextoDePdf(string rutaArchivo)
        {
            var textoProcesado = new StringBuilder();
            using (PdfDocument document = PdfDocument.Open(rutaArchivo))
            {
                foreach (Page page in document.GetPages())
                {
                    var lines = page.GetWords()
                                    .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 2))
                                    .OrderByDescending(g => g.Key);

                    foreach (var line in lines)
                    {
                        string lineText = string.Join(" ", line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                        textoProcesado.AppendLine(lineText);
                    }
                    textoProcesado.AppendLine();
                }
            }
            return textoProcesado.ToString();
        }

        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Helvetica"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("LIC. SERGIO AGUILASOCHO GARCÍA").Bold().FontSize(14);
                        col.Item().Text("NOTARIA PUBLICA No. 215").FontSize(12);
                        col.Item().PaddingTop(10).Text("Blvd. Romualdo Ruiz Payan 210, Fracc, Miravalle, Guasave, Sinaloa, C.P. 81048").FontSize(9);
                        col.Item().Text("Tel: (687) 883.64.64 | saguilasocho@notario215.com").FontSize(9);
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().Text("UNIDAD DE INTELIGENCIA FINANCIERA").Bold().Underline();
                        col.Item().Text("PRESENTE:").Bold();

                        col.Item().PaddingTop(25).Text(text =>
                        {
                            text.Justify();
                            text.Span("Con fundamento en lo dispuesto por el numeral 17, fracción 12 apartado A, de la Ley Federal para la Identificación de Operaciones con Recursos de Procedencia Ilícita, sus demás artículos correlativos del Reglamento de la materia, así como los artículos 27 y 38 de las Reglas de Carácter General de dichos ordenamientos, entratándose de actividades vulnerables realizadas ante notario público, hago constar que, con esta fecha, el personal de esta notaría a mi cargo realizó la búsqueda y verificó en las listas proporcionadas por la Unidad de Inteligencia Financiera del Servicio de Administración Tributaria, las cuales fueron descargadas directamente de su portal https://sppld.sat.gob.mx/pld/index.html, y después de cotejar dichos listados, se encontró el siguiente resultado:");
                        });

                        // --- INICIO DE LA SECCIÓN MODIFICADA ---
                        // Se utiliza el prefijo "SR.(A)" y se pone todo en negritas.
                        col.Item().PaddingTop(25).Text(text =>
                        {
                            text.Span("SR.(A) ").Bold();
                            text.Span(terminoBuscado).Bold();
                        });
                        // --- FIN DE LA SECCIÓN MODIFICADA ---

                        col.Item().PaddingTop(15).Text(text =>
                        {
                            if (esAprobatoria)
                            {
                                text.Span("NO").Bold();
                                text.Span(" se encontró dentro del listado de personas vinculadas al lavado de dinero, crimen organizado o financiamiento al terrorismo.");
                            }
                            else
                            {
                                text.Span("SI").Bold();
                                text.Span(" se encontró dentro del listado de personas vinculadas al lavado de dinero, crimen organizado o financiamiento al terrorismo.");
                            }
                        });

                        col.Item().PaddingTop(80).AlignCenter().Text("Atentamente:");
                        col.Item().PaddingTop(40).AlignCenter().Text("_________________________");
                        col.Item().AlignCenter().Text("Lic. Sergio Aguilasocho García.");
                        col.Item().AlignCenter().Text("Notario Público No. 215");
                    });
                });
            })
            .GeneratePdf(rutaGuardado);
        }

        // El método GetGenderPrefix ya no es necesario y ha sido eliminado.
    }
}