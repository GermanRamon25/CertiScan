using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Linq;
using System.Collections.Generic;

namespace CertiScan.Services
{
    public class PdfSatService
    {
        static PdfSatService()
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Advertencia] No se pudo inicializar QuestPDF en SAT: {ex.Message}");
            }
        }

        public string ExtraerTextoDePdf(string rutaArchivo)
        {
            var textoProcesado = new StringBuilder();
            try
            {
                using (PdfDocument document = PdfDocument.Open(rutaArchivo))
                {
                    foreach (Page page in document.GetPages())
                    {
                        var words = page.GetWords();
                        if (words.Any())
                        {
                            var lines = words.GroupBy(w => Math.Round(w.BoundingBox.Bottom, 2))
                                             .OrderByDescending(g => g.Key);
                            foreach (var line in lines)
                            {
                                string lineText = string.Join(" ", line.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                                textoProcesado.AppendLine(lineText);
                            }
                        }
                        textoProcesado.AppendLine();
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error al extraer texto SAT: {ex.Message}";
            }
            return textoProcesado.ToString();
        }

        public void GenerarReporteSat(string rutaGuardado, string rfcBuscado, bool esLimpio, List<string> archivosAnalizados, DatosNotaria datos)
        {
            archivosAnalizados = archivosAnalizados ?? new List<string>();

            if (datos == null)
            {
                datos = new DatosNotaria { NombreNotario = "DATO NO CONFIGURADO", NumeroNotaria = "0", DireccionCompleta = "CONFIGURAR EN MENU NOTARIA", DatosContacto = "" };
            }

            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imagenes", "CERTISCAN.LOGO.png");
            byte[] logoData = null;
            try { if (File.Exists(logoPath)) logoData = File.ReadAllBytes(logoPath); } catch { }

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Helvetica"));

                        // --- ENCABEZADO ---
                        page.Header().Column(headerCol =>
                        {
                            headerCol.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
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
                            headerCol.Item().PaddingTop(10).LineHorizontal(1).LineColor("#691C32"); // Color Bordo SAT
                        });

                        // --- CONTENIDO ---
                        page.Content().PaddingVertical(20).Column(col =>
                        {
                            col.Item().Text("CONSTANCIA DE VERIFICACIÓN - ARTÍCULO 69, 69-B y 69-B Bis  ").Bold().FontSize(13).FontColor("#691C32");

                            col.Item().PaddingTop(20).Text(text =>
                            {
                                text.Justify();
                                text.Span("El suscrito Notario Público, hace constar que realizó la busqueda del compareciente en el listado de contribuyentes incumplidos por motivo de publicación a que se refieren los Artículos 69, 69-B y 69-B Bis del Código Fiscal de la Federación");
                                text.Span(", y que se encuentran contenidos en el portal del Servicio de Administración Tributaria SAT. Contribuyentes publicados y acciones contra la delincuencia que dicha busqueda arrojo los siguientes resultados.");
                            });

                            col.Item().PaddingTop(20).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(innerCol =>
                            {
                                innerCol.Item().Text(text => {
                                    text.Span("NOMBRE O DENOMINACIÓN: ").Bold();
                                    text.Span(rfcBuscado.ToUpper());
                                });

                                innerCol.Item().PaddingTop(5).Text(text => {
                                    text.Span("FECHA DE CONSULTA: ").Bold();
                                    text.Span($"{DateTime.Now:dd/MM/yyyy HH:mm} horas");
                                });
                            });

                            // --- RESULTADO ---
                            col.Item().PaddingTop(20).Text(text =>
                            {
                                if (esLimpio)
                                {
                                    text.Span("RESULTADO: SIN COINCIDENCIAS (LIMPIO)").Bold().FontColor(Colors.Green.Medium);
                                    text.Span("\n\nSe informa que después de cotejar la base de datos de los listados publicados por el Servicio de Administración Tributaria (SAT), ");
                                    text.Span("NO").Bold();
                                    text.Span(" se localizó registro alguno que vincule al contribuyente antes mencionado con las situaciones jurídicas previstas en el numeral 69, 69-B Y 69 B Bis del Código Fiscal de la Federación.");
                                }
                                else
                                {
                                    text.Span("RESULTADO: CON COINCIDENCIAS (HALLAZGO)").Bold().FontColor(Colors.Red.Medium);
                                    text.Span("\n\nSe informa que después de cotejar la base de datos de los listados publicados por el Servicio de Administración Tributaria (SAT),");
                                    text.Span("SI").Bold();
                                    text.Span("se localizó registro alguno que vincule al contribuyente antes mencionado con las situaciones jurídicas previstas en el numeral 69, 69-B Y 69 B Bis del Código Fiscal de la Federación.");
                                }
                            });

                            // --- ARCHIVOS ANALIZADOS ---
                            if (archivosAnalizados != null && archivosAnalizados.Any())
                            {
                                col.Item().PaddingTop(20).Column(fileCol =>
                                {
                                    fileCol.Item().Text("Listados Analizados:").Bold().FontSize(10);
                                    foreach (var file in archivosAnalizados)
                                    {
                                        fileCol.Item().Text($"- {file}").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    }
                                });
                            }

                            // --- FIRMA ---
                            col.Item().PaddingTop(60).AlignCenter().Column(signatureCol =>
                            {
                                signatureCol.Item().AlignCenter().Text("Atentamente:");
                                signatureCol.Item().PaddingTop(40).AlignCenter().Text("_________________________");
                                signatureCol.Item().AlignCenter().Text("LIC. " + datos.NombreNotario.ToUpper());
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
                throw new Exception($"Error al generar el reporte SAT: {ex.Message}");
            }
        }
    }
}