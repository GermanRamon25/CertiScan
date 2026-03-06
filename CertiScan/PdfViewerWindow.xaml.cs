using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using System.Text.RegularExpressions; // Necesario para limpiar el nombre

namespace CertiScan
{
    public partial class PdfViewerWindow : Window
    {
        private readonly string _tempPdfPath;
        // Nueva propiedad para recibir el nombre del buscado
        public string TerminoBuscado { get; set; } = "SIN_NOMBRE";

        public PdfViewerWindow(string pdfPath, string nombrePersona = "")
        {
            InitializeComponent();
            _tempPdfPath = pdfPath;
            TerminoBuscado = nombrePersona; // Recibimos el nombre desde el MainViewModel

            InitializeWebView(pdfPath);
        }

        private async void InitializeWebView(string pdfPath)
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.NavigationCompleted += WebView_NavigationCompleted;
                webView.CoreWebView2.Navigate(new Uri(pdfPath).AbsoluteUri);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error crítico al inicializar el visor: {ex.Message}", "Error");
                this.Close();
            }
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            await webView.ExecuteScriptAsync("window.print();");
        }

        // --- CORRECCIÓN: GUARDADO AUTOMÁTICO CON NOMBRE Y FECHA ---
        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Limpiamos el nombre de espacios o caracteres raros
            string nombreLimpio = TerminoBuscado.Replace(" ", "_").ToUpper();
            if (string.IsNullOrWhiteSpace(nombreLimpio)) nombreLimpio = "CONSULTA";

            // 2. Generamos Fecha y Hora (ej: 06032026_1045)
            string fechaHora = DateTime.Now.ToString("ddMMyyyy_HHmm");

            // 3. Formato final: Constancia_NOMBRE_FECHA_HORA.pdf
            string nombreSugerido = $"Constancia_{nombreLimpio}_{fechaHora}.pdf";

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo PDF (.pdf)|*.pdf",
                Title = "Guardar Constancia Como...",
                FileName = nombreSugerido // <--- ESTO PONE EL NOMBRE AUTOMÁTICO
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(_tempPdfPath, saveFileDialog.FileName, true);
                    MessageBox.Show($"Constancia guardada exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void WebView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            webView.ExecuteScriptAsync("document.querySelector('viewer-pdf-toolbar').shadowRoot.querySelector('#toolbar').style.display = 'none';");
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try
            {
                if (File.Exists(_tempPdfPath)) File.Delete(_tempPdfPath);
            }
            catch { }
        }
    }
}