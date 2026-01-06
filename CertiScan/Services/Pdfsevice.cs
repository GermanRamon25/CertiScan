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
    // Clase para transportar los datos de la notaría al documento
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
                // Configuración de licencia comunitaria para QuestPDF
                QuestPDF.Settings.License = LicenseType.Community;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Advertencia] No se pudo inicializar QuestPDF: {ex.Message}");
            }
        }

        // Método para extraer texto de PDFs
        public string ExtraerTextoDePdf(string rutaArchivo)
        {
            var textoProcesado = new StringBuilder();
            try
            {
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
            }
            catch (Exception ex)
            {
                return $"Error al extraer texto: {ex.Message}";
            }
            return textoProcesado.ToString();
        }

        // --- MÉTODO 1: EL MOTOR (Genera el diseño del PDF con los datos recibidos) ---
        // Este método utiliza el objeto 'datos' enviado desde el MainViewModel
        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria, List<string> nombresArchivosEncontrados, DatosNotaria datos)
        {
            nombresArchivosEncontrados = nombresArchivosEncontrados ?? new List<string>();

            // Validación de seguridad por si el objeto datos llega nulo
            if (datos == null)
            {
                datos = new DatosNotaria
                {
                    NombreNotario = "DATO NO CONFIGURADO",
                    NumeroNotaria = "0",
                    DireccionCompleta = "CONFIGURAR EN MENU NOTARIA",
                    DatosContacto = ""
                };
            }

            // Carga de Logo
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes", "CERTISCAN.LOGO.png");
            byte[] logoData = null;
            try { if (File.Exists(logoPath)) logoData = File.ReadAllBytes(logoPath); }
            catch { /* Logo opcional */ }

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50);
                        page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Helvetica"));

                        // --- ENCABEZADO DINÁMICO ---
                        page.Header().Column(headerCol =>
                        {
                            headerCol.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    // Se usan las propiedades del objeto 'datos'
                                    col.Item().Text(datos.NombreNotario.ToUpper()).Bold().FontSize(14);
                                    col.Item().Text($"NOTARIA PUBLICA No. {datos.NumeroNotaria}").FontSize(12);
                                    col.Item().PaddingTop(10).Text(datos.DireccionCompleta).FontSize(9);
                                    col.Item().Text(datos.DatosContacto).FontSize(9);
                                });
                                if (logoData != null)
                                {
                                    row.ConstantItem(60).AlignRight().AlignTop().Image(logoData).FitArea();
                                }
                            });
                            headerCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        });

                        // --- CONTENIDO PRINCIPAL ---
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
                                        text.Span("Coincidencia detectada en: ").Italic().FontSize(10);
                                        text.Span(string.Join(", ", nombresArchivosEncontrados)).Italic().FontSize(10).Bold();
                                    }
                                }
                            });

                            // --- FIRMA DINÁMICA ---
                            col.Item().PaddingTop(60).AlignCenter().Column(signatureCol =>
                            {
                                signatureCol.Spacing(5);
                                signatureCol.Item().AlignCenter().Text("Atentamente:");
                                signatureCol.Item().PaddingTop(40).AlignCenter().Text("_________________________");
                                // Se usan las propiedades del objeto 'datos' para la firma
                                signatureCol.Item().AlignCenter().Text(datos.NombreNotario.ToUpper() + ".");
                                signatureCol.Item().AlignCenter().Text($"Notario Público No. {datos.NumeroNotaria}");
                            });
                        });

                        page.Footer().AlignLeft().Text(text =>
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

        // --- MÉTODO 2: EL PUENTE (Opcional, busca en BD si no se pasan datos) ---
        // Este método busca los datos en la BD usando el NotariaId de la sesión
        public void GenerarConstancia(string rutaGuardado, string terminoBuscado, bool esAprobatoria, List<string> nombresArchivosEncontrados)
        {
            if (SessionService.UsuarioLogueado != null)
            {
                var db = new DatabaseService();
                var infoDB = db.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);

                if (infoDB != null)
                {
                    var datosParaPdf = new DatosNotaria
                    {
                        NombreNotario = infoDB.NombreNotario,
                        NumeroNotaria = infoDB.NumeroNotaria,
                        DireccionCompleta = infoDB.Direccion,
                        DatosContacto = $"Tel: {infoDB.Telefono} | Email: {infoDB.Email}"
                    };

                    GenerarConstancia(rutaGuardado, terminoBuscado, esAprobatoria, nombresArchivosEncontrados, datosParaPdf);
                    return;
                }
            }

            MessageBox.Show("No se pudo recuperar la información de la notaría. Verifique su configuración.", "Error de Datos", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}