using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using QuestPDF.Drawing;

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
            // Esta función está deshabilitada porque quitamos la librería iText.
            return string.Empty;
        }

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

                    // --- Encabezado ---
                    page.Header().Column(col =>
                    {
                        col.Item().Text("LIC. SERGIO AGUILASOCHO GARCÍA").Bold().FontSize(14);
                        col.Item().Text("NOTARIA PUBLICA No. 215").FontSize(12);
                        col.Item().PaddingTop(10).Text("Blvd. Romualdo Ruiz Payan 210, Fracc, Miravalle, Guasave, Sinaloa, C.P. 81048").FontSize(9);
                        col.Item().Text("Tel: (687) 883.64.64 | saguilasocho@notario215.com").FontSize(9);
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    });

                    // --- Contenido Principal ---
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().AlignRight().Text($"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(10);
                        col.Item().PaddingTop(15).Text(text =>
                        {
                            text.Span("Término de Búsqueda: ").Bold();
                            text.Span(terminoBuscado);
                        });

                        // --- CORRECCIÓN FINAL ---
                        // El método correcto para justificar el texto es .Justify()
                        col.Item().PaddingTop(20).Text(text =>
                        {
                            text.Justify(); // <-- ESTA ES LA CORRECCIÓN
                            text.Span(cuerpoDelMensaje);
                        });

                        // Espacio para la firma
                        col.Item().AlignCenter().PaddingTop(100).Text("_________________________\nFirma del Responsable de Cumplimiento");
                    });

                    // --- Pie de página (opcional) ---
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