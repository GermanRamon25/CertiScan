using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using QuestPDF.Drawing;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

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
                    textoProcesado.Append(page.Text);
                    textoProcesado.Append(" ");
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

                    // El encabezado con los datos de la notaría se mantiene igual.
                    page.Header().Column(col =>
                    {
                        col.Item().Text("LIC. SERGIO AGUILASOCHO GARCÍA").Bold().FontSize(14);
                        col.Item().Text("NOTARIA PUBLICA No. 215").FontSize(12);
                        col.Item().PaddingTop(10).Text("Blvd. Romualdo Ruiz Payan 210, Fracc, Miravalle, Guasave, Sinaloa, C.P. 81048").FontSize(9);
                        col.Item().Text("Tel: (687) 883.64.64 | saguilasocho@notario215.com").FontSize(9);
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    });

                    // El contenido principal ahora usa el nuevo formato.
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().Text("UNIDAD DE INTELIGENCIA FINANCIERA").Bold().Underline();
                        col.Item().Text("PRESENTE:").Bold();

                        col.Item().PaddingTop(25).Text(text =>
                        {
                            text.Justify();
                            text.Span("Con fundamento en lo dispuesto por el numeral 17, apartado A, de la Ley Federal para la Identificación de Operaciones con Recursos de Procedencia Ilícita, sus demás artículos correlativos del Reglamento de la materia, así como los artículos 27 y 38 de las Reglas de Carácter General de dichos ordenamientos, hago constar que, con esta fecha, el personal de esta notaría a mi cargo realizó la búsqueda y verificó en las listas proporcionadas por la Unidad de Inteligencia Financiera del Servicio de Administración Tributaria, las cuales fueron descargadas directamente de su portal https://sppld.sat.gob.mx/pld/index.html, y después de cotejar dichos listados, se encontró el siguiente resultado:");
                        });

                        // Se añade la primera línea por separado.
                        col.Item().PaddingTop(25).Text($"El señor: {terminoBuscado}");

                        // Se añade la segunda línea usando el descriptor de texto para poder aplicar formato.
                        col.Item().Text(text =>
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

                        // Firma
                        col.Item().PaddingTop(80).AlignCenter().Text("Atentamente:");
                        col.Item().PaddingTop(40).AlignCenter().Text("_________________________");
                        col.Item().AlignCenter().Text("Lic. Sergio Aguilasocho García.");
                        col.Item().AlignCenter().Text("Notario Público 215.");
                    });

                    // --- SECCIÓN DE PIE DE PÁGINA ELIMINADA ---
                    // Ya no se mostrará el número de página.
                });
            })
            .GeneratePdf(rutaGuardado);
        }
    }
}