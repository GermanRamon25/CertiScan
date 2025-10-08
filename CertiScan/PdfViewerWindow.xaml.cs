using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace CertiScan
{
    public partial class PdfViewerWindow : Window
    {
        private readonly string _tempPdfPath;

        public PdfViewerWindow(string pdfPath)
        {
            InitializeComponent();
            _tempPdfPath = pdfPath;
            // Asegurarse que el WebView2 esté listo antes de cargar el PDF
            webView.NavigationCompleted += WebView_NavigationCompleted;
            // Inicializa y carga el archivo
            webView.Source = new Uri(pdfPath);
        }

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