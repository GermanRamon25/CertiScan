using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.IO;
using System.Text;

// Se eliminaron los 'using' de System.Windows que causaban el conflicto.
namespace CertiScan.Services
{
    public class PdfService
    {
        public string ExtraerTextoDePdf(string rutaArchivo)
        {
            var textoProcesado = new StringBuilder();
            using (var pdfDoc = new PdfDocument(new PdfReader(rutaArchivo)))
            {
                for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    textoProcesado.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
                }
            }
            return textoProcesado.ToString();
        }

        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria)
        {
            using (var writer = new PdfWriter(rutaGuardado))
            {
                using (var pdf = new PdfDocument(writer))
                {
                    var document = new Document(pdf);

                    string resultadoTexto = esAprobatoria
                        ? "APROBATORIA: No se encontraron coincidencias en la base de datos documental."
                        : "DENEGADA: Se encontraron una o más coincidencias en la base de datos documental.";

                    // Se crea una fuente en negrita para el título.
                    PdfFont fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                    // Ahora no hay ambigüedad, porque solo se conocen las clases de iText.
                    Paragraph titulo = new Paragraph("CONSTANCIA DE BÚSQUEDA EN LISTAS")
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFont(fontBold) // Se aplica la fuente en negrita
                        .SetFontSize(16);
                    document.Add(titulo);

                    Paragraph fecha = new Paragraph($"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                        .SetTextAlignment(TextAlignment.RIGHT);
                    document.Add(fecha);

                    document.Add(new Paragraph("\n"));
                    document.Add(new Paragraph($"Término de Búsqueda: {terminoBuscado}"));
                    document.Add(new Paragraph($"Resultado: {resultadoTexto}"));
                    document.Add(new Paragraph("\n\n\n"));

                    Paragraph lineaFirma = new Paragraph("_________________________")
                        .SetTextAlignment(TextAlignment.CENTER);
                    document.Add(lineaFirma);

                    Paragraph textoFirma = new Paragraph("Firma del Responsable")
                        .SetTextAlignment(TextAlignment.CENTER);
                    document.Add(textoFirma);
                }
            }
        }
    }
}