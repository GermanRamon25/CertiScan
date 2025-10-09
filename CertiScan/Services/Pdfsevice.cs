using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using QuestPDF.Drawing;
using System.Text; // Necesario para StringBuilder
using UglyToad.PdfPig; // Librería nueva para leer PDF
using UglyToad.PdfPig.Content; // Librería nueva para leer PDF

namespace CertiScan.Services
{
    public class PdfService
    {
        static PdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // --- MÉTODO ACTUALIZADO CON LA NUEVA LIBRERÍA PDFPIG ---
        public string ExtraerTextoDePdf(string rutaArchivo)
        {
            var textoProcesado = new StringBuilder();

            // Usamos PdfPig para abrir y leer el documento
            using (PdfDocument document = PdfDocument.Open(rutaArchivo))
            {
                foreach (Page page in document.GetPages())
                {
                    // Obtenemos el texto de cada página y lo añadimos
                    textoProcesado.Append(page.Text);
                    textoProcesado.Append(" "); // Añadimos un espacio entre páginas
                }
            }
            return textoProcesado.ToString();
        }

        // (El método para generar constancias no cambia, sigue usando QuestPDF)
        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria)
        {
            string cuerpoDelMensaje;
            if (esAprobatoria)
            {
                cuerpoDelMensaje = "Por medio de la presente, se hace constar que, tras realizar una búsqueda exhaustiva en la base de datos documental interna a la fecha y hora de emisión de este documento, NO SE ENCONTRARON coincidencias o registros asociados al nombre consultado. Esta búsqueda se realizó como parte de los procedimientos de debida diligencia para la prevención de operaciones con recursos de procedencia ilícita.";
            }
            else
            {
                cuerpoDelMensaje = "Por medio de la presente, se informa que, tras realizar una búsqueda en la base de datos documental interna a la fecha y hora de emisión de este documento, SE ENCONTRARON una o más posibles coincidencias o registros asociados al nombre consultado. Se recomienda aplicar un procedimiento de debida diligencia ampliada para confirmar la identidad del sujeto y determinar las acciones correspondientes, conforme a la normativa en materia de prevención de operaciones con recursos de procedencia ilícita.";
            }

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(30);
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
                        col.Item().AlignRight().Text($"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(10);
                        col.Item().PaddingTop(15).Text(text =>
                        {
                            text.Span("Término de Búsqueda: ").Bold();
                            text.Span(terminoBuscado);
                        });

                        col.Item().PaddingTop(20).Text(text =>
                        {
                            text.Justify();
                            text.Span(cuerpoDelMensaje);
                        });

                        col.Item().AlignCenter().PaddingTop(100).Text("_________________________\nFirma del Responsable de Cumplimiento");
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                        });
                });
            })
            .GeneratePdf(rutaGuardado);
        }
    }
}