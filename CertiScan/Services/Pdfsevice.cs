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
    // Nueva clase para transportar los datos manuales del cliente
    public class DatosNotaria
    {
        public string NombreNotario { get; set; }
        public string NumeroNotaria { get; set; }
        public string DireccionCompleta { get; set; }
        public string DatosContacto { get; set; }
    }

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

        // --- MÉTODO ACTUALIZADO: Acepta el objeto 'datos' ---
        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria, List<string> nombresArchivosEncontrados, DatosNotaria datos)
        {
            nombresArchivosEncontrados = nombresArchivosEncontrados ?? new List<string>();

            // Si por alguna razón 'datos' llega nulo, usamos valores por defecto para evitar errores
            datos = datos ?? new DatosNotaria
            {
                NombreNotario = "NO ESPECIFICADO",
                NumeroNotaria = "0",
                DireccionCompleta = "SIN DIRECCIÓN",
                DatosContacto = ""
            };

            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes", "CERTISCAN.LOGO.png");
            byte[] logoData = null;
            try { if (File.Exists(logoPath)) logoData = File.ReadAllBytes(logoPath); }
            catch (Exception ex) { MessageBox.Show($"Error al cargar logo: {ex.Message}"); }

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50);
                        page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Helvetica"));

                        // --- Encabezado Dinámico ---
                        page.Header().Column(headerCol =>
                        {
                            headerCol.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    // USAMOS LOS DATOS INGRESADOS MANUALMENTE
                                    col.Item().Text(datos.NombreNotario.ToUpper()).Bold().FontSize(14);
                                    col.Item().Text($"NOTARIA PUBLICA No. {datos.NumeroNotaria}").FontSize(12);
                                    col.Item().PaddingTop(10).Text(datos.DireccionCompleta).FontSize(9);
                                    col.Item().Text(datos.DatosContacto).FontSize(9);
                                });
                                if (logoData != null)
                                {
                                    row.ConstantItem(40).AlignRight().AlignTop().Image(logoData).FitArea();
                                }
                            });
                            headerCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        });

                        // --- Contenido Principal ---
                        page.Content().PaddingVertical(20).Column(col =>
                        {
                            col.Item().Text("UNIDAD DE INTELIGENCIA FINANCIERA").Bold().Underline();
                            col.Item().Text("PRESENTE:").Bold();

                            col.Item().PaddingTop(25).Text(text =>
                            {
                                text.Justify();
                                text.Span("Con fundamento en lo dispuesto por el artículo 115 de la Ley de Instituciones de Crédito vigente relativas a la lista de personas bloqueadas y atendiendo a la obligación del suscrito notario impuesta por diversas disposiciones legales tales como el numeral 17, fracción XII, apartado A, de la Ley Federal para la Identificación de Operaciones con Recursos de Procedencia Ilícita, sus demás artículos correlativos del Reglamento de la materia, así como los artículos 27 y 38 de las Reglas de Carácter General de dichos ordenamientos, hago constar que el personal de esta notaría a mi cargo con esta fecha ");
                                text.Span($"{DateTime.Now:dd/MM/yyyy}").Bold();
                                text.Span(" realizó la búsqueda y verificó en las listas proporcionadas por la Unidad de Inteligencia Financiera del Servicio de Administración Tributaria...");
                            });

                            col.Item().PaddingTop(25).Text(text => {
                                text.Span("Nombre o Denominación: ").Bold();
                                text.Span(terminoBuscado).Bold();
                            });

                            col.Item().PaddingTop(15).Text(text =>
                            {
                                if (esAprobatoria)
                                {
                                    text.Span("NO").Bold();
                                    text.Span(" se encontró dentro del listado de personas vinculadas al lavado de dinero...");
                                }
                                else
                                {
                                    text.Span("SI").Bold();
                                    text.Span(" se encontró dentro del listado...");
                                    if (nombresArchivosEncontrados.Any())
                                    {
                                        text.EmptyLine();
                                        text.Span("Coincidencia en: ").Italic().FontSize(10);
                                        text.Span(string.Join(", ", nombresArchivosEncontrados)).Italic().FontSize(10).Bold();
                                    }
                                }
                            });

                            // --- Firma Dinámica ---
                            col.Item()
                               .PaddingTop(60)
                               .AlignCenter()
                               .Column(signatureCol =>
                               {
                                   signatureCol.Spacing(5);
                                   signatureCol.Item().AlignCenter().Text("Atentamente:");
                                   signatureCol.Item().PaddingTop(40).AlignCenter().Text("_________________________");
                                   // USAMOS EL NOMBRE DEL NOTARIO INGRESADO
                                   signatureCol.Item().AlignCenter().Text(datos.NombreNotario.ToUpper() + ".");
                                   signatureCol.Item().AlignCenter().Text($"Notario Público No. {datos.NumeroNotaria}");
                               });
                        });

                        page.Footer()
                            .AlignLeft()
                            .Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                                text.Span($"© {DateTime.Now.Year} CertiScan | Sistema de Verificación Profesional");
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

        // Sobrecarga para mantener compatibilidad con el resto del sistema si es necesario
        // En Pdfsevice.cs, cambia la sobrecarga del final por esta:
        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria, List<string> nombresArchivosEncontrados)
        {
            // En lugar de datos fijos, intenta sacarlos de la sesión actual si existen
            if (SessionService.UsuarioLogueado != null)
            {
                var db = new DatabaseService();
                var info = db.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);

                var datos = new DatosNotaria
                {
                    NombreNotario = info.NombreNotario,
                    NumeroNotaria = info.NumeroNotaria,
                    DireccionCompleta = info.Direccion,
                    DatosContacto = $"Tel: {info.Telefono} | {info.Email}"
                };
                GenerarConstancia(rutaGuardado, terminoBuscado, esAprobatoria, nombresArchivosEncontrados, datos);
            }
        }
    }
}