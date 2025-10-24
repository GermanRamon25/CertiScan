using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core; // Se mantiene, necesario para CoreWebView2NavigationCompletedEventArgs

namespace CertiScan
{
    public partial class PdfViewerWindow : Window
    {
        private readonly string _tempPdfPath;

        public PdfViewerWindow(string pdfPath)
        {
            InitializeComponent();
            _tempPdfPath = pdfPath;

            // Llama al nuevo método asíncrono para inicializar WebView2 de forma segura
            InitializeWebView(pdfPath);
        }

        // --- MÉTODO NUEVO: Inicialización Asíncrona para compatibilidad con Windows 8.1 ---
        private async void InitializeWebView(string pdfPath)
        {
            try
            {
                // 1. Asegurarse que el WebView2 esté inicializado antes de usarlo
                await webView.EnsureCoreWebView2Async(null);

                // 2. Mover la lógica de carga aquí
                webView.NavigationCompleted += WebView_NavigationCompleted;
                webView.Source = new Uri(pdfPath);
            }
            catch (Exception ex)
            {
                // Capturar errores de inicialización (comunes en Win 8.1 sin el Runtime o si hay fallas de dependencia)
                MessageBox.Show(
                    $"Error crítico al inicializar el visor de PDF (WebView2).\n" +
                    $"Asegúrese de tener instalado el Microsoft Edge WebView2 Runtime y las librerías C++.\n\n" +
                    $"Detalle: {ex.Message}",
                    "Error de Compatibilidad",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                this.Close(); // Cerrar la ventana si el visor no puede funcionar
            }
        }
        // --- FIN DEL MÉTODO NUEVO ---

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            // Ejecuta el diálogo de impresión del navegador
            await webView.ExecuteScriptAsync("window.print();");
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo PDF (.pdf)|*.pdf",
                Title = "Guardar Constancia Como...",
                FileName = Path.GetFileName(_tempPdfPath)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(_tempPdfPath, saveFileDialog.FileName, true);
                    MessageBox.Show($"Constancia guardada exitosamente en:\n{saveFileDialog.FileName}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void WebView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            // Opcional: Ocultar la barra de herramientas que muestra el navegador en el PDF
            webView.ExecuteScriptAsync("document.querySelector('viewer-pdf-toolbar').shadowRoot.querySelector('#toolbar').style.display = 'none';");
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Limpia y borra el archivo temporal al cerrar la ventana
            try
            {
                if (File.Exists(_tempPdfPath))
                {
                    File.Delete(_tempPdfPath);
                }
            }
            catch (Exception ex)
            {
                // Manejar el error si el archivo no se puede borrar (poco probable)
                Console.WriteLine($"No se pudo borrar el archivo temporal: {ex.Message}");
            }
        }
    }
}