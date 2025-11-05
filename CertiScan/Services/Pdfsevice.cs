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
using System.Windows;
using System.Collections.Generic;

namespace CertiScan.Services
{
    public class PdfService
    {
        static PdfService()
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
            }
            catch (Exception ex)
            {
                // Si falla la inicialización, no se detiene la aplicación
                Console.WriteLine($"[Advertencia] No se pudo inicializar QuestPDF: {ex.Message}");
            }
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

        // --- Sobrecarga #1: Usada por MainViewModel
        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria, List<string> nombresArchivosEncontrados)
        {
            GenerarConstancia(rutaGuardado, terminoBuscado, esAprobatoria, DateTime.Now, nombresArchivosEncontrados ?? new List<string>());
        }

        // --- Sobrecarga #2: Usada por HistoryViewModel (Principal)
        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria, DateTime fechaDeBusqueda, List<string> nombresArchivosEncontrados)
        {
            nombresArchivosEncontrados = nombresArchivosEncontrados ?? new List<string>();

            // --- Carga del Logo ---
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes", "LOGO_CERTISCAN.png");
            byte[] logoData = null;
            try
            {
                if (File.Exists(logoPath))
                    logoData = File.ReadAllBytes(logoPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar logo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50);
                        page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Helvetica"));

                        // --- Encabezado ---
                        page.Header().Column(headerCol =>
                        {
                            headerCol.Item().Row(row =>
                            {
                                // Info Notaría
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("LIC. RICARDO AGUILASOCHO RUBIO").Bold().FontSize(14);
                                    col.Item().Text("NOTARIA PUBLICA No. 138").FontSize(12);
                                    col.Item().PaddingTop(10).Text("Av. Antonio Norzagaray #1127 y Diaz de León, Guasave, Sinaloa,MEXICO, C.P. 81000").FontSize(9);
                                    col.Item().Text("Tel: (687) 872 37 80 | ricardoaguilasocho@prodigy.net.mx").FontSize(9);
                                });
                                // Logo
                                if (logoData != null)
                                    row.ConstantItem(70).AlignRight().AlignTop().Image(logoData).FitArea();
                            });
                            // Línea separadora
                            headerCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        });

                        // --- Contenido Principal ---
                        page.Content().PaddingVertical(20).Column(col =>
                        {
                            col.Item().Text("UNIDAD DE INTELIGENCIA FINANCIERA").Bold().Underline();
                            col.Item().Text("PRESENTE:").Bold();

                            // Texto introductorio
                            col.Item().PaddingTop(25).Text(text =>
                            {
                                text.Justify();
                                text.Span("Con fundamento en lo dispuesto por el numeral 17, fracción XII, apartado A, de la Ley Federal para la Identificación de Operaciones con Recursos de Procedencia Ilícita, sus demás artículos correlativos del Reglamento de la materia, así como los artículos 27 y 38 de las Reglas de Carácter General de dichos ordenamientos, hago constar que el personal de esta notaría a mi cargo con esta fecha ");
                                text.Span($"{fechaDeBusqueda:dd/MM/yyyy}").Bold();
                                text.Span(" realizó la búsqueda y verificó en las listas proporcionadas por la Unidad de Inteligencia Financiera del Servicio de Administración Tributaria, las cuales fueron descargadas directamente de su portal https://sppld.sat.gob.mx/pld/index.html, y después de cotejar dichos listados, se encontró el siguiente resultado:");
                            });

                            // Nombre buscado
                            col.Item().PaddingTop(25).Text(text => { text.Span("SR.(A) ").Bold(); text.Span(terminoBuscado).Bold(); });

                            // Resultado (SI/NO encontrado y archivos)
                            col.Item().PaddingTop(15).Text(text =>
                            {
                                if (esAprobatoria)
                                {
                                    text.Span("NO").Bold();
                                    text.Span(" se encontró dentro del listado de personas vinculadas al lavado de dinero, crimen organizado o financiamiento al terrorismo.");
                                    text.EmptyLine();
                                    text.Span("No se encontró coincidencia en ningún archivo UIF cargado en el sistema.").Italic().FontSize(10);
                                }
                                else
                                {
                                    text.Span("SI").Bold();
                                    text.Span(" se encontró dentro del listado de personas vinculadas al lavado de dinero, crimen organizado o financiamiento al terrorismo.");
                                    if (nombresArchivosEncontrados.Any())
                                    {
                                        text.EmptyLine();
                                        text.Span("La coincidencia fue encontrada en el/los siguiente(s) archivo(s) UIF: ").Italic().FontSize(10);
                                        text.Span(string.Join(", ", nombresArchivosEncontrados)).Italic().FontSize(10).Bold();
                                    }
                                }
                            });

                            // Firma Agrupada y Centrada
                            col.Item()
                               .PaddingTop(60)
                               .AlignCenter()
                               .Column(signatureCol =>
                               {
                                   signatureCol.Spacing(5);
                                   signatureCol.Item().AlignCenter().Text("Atentamente:");
                                   signatureCol.Item().PaddingTop(40).AlignCenter().Text("_________________________");
                                   signatureCol.Item().AlignCenter().Text("Lic. RICARDO AGUILASOCHO RUBIO .");
                                   signatureCol.Item().AlignCenter().Text("Notario Público No. 138");
                               });
                        });

                        // --- Pie de Página ---
                        page.Footer()
                            .AlignLeft()
                            .Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                                text.Span("© ");
                                text.Span($"{DateTime.Now.Year}");
                                text.Span(" CertiScan | Desarrollado por: Germán Ramón Soto Valenzuela, Alondra Anaid Villegas Iturrios");
                            });
                    });
                })
                .GeneratePdf(rutaGuardado);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar el PDF: {ex.Message}", "Error PDF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Sobrecargas antiguas ---
        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria, DateTime fechaDeBusqueda)
        {
            GenerarConstancia(rutaGuardado, terminoBuscado, esAprobatoria, fechaDeBusqueda, new List<string>());
        }

        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria)
        {
            GenerarConstancia(rutaGuardado, terminoBuscado, esAprobatoria, DateTime.Now, new List<string>());
        }
    }
}
