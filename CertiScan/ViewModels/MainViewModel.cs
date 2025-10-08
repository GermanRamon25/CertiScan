using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CertiScan.Models;
using CertiScan.Services;

namespace CertiScan.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;

        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda
        {
            get => _terminoBusqueda;
            set => SetProperty(ref _terminoBusqueda, value);
        }

        public ObservableCollection<Documento> ResultadosBusqueda { get; set; }

        private Documento _selectedDocumento;
        public Documento SelectedDocumento
        {
            get => _selectedDocumento;
            set
            {
                if (SetProperty(ref _selectedDocumento, value) && value != null)
                {
                    LoadPdfContent(value.Id);
                }
            }
        }

        private string _pdfContent;
        public string PdfContent
        {
            get => _pdfContent;
            set => SetProperty(ref _pdfContent, value);
        }

        public IRelayCommand CargarPdfCommand { get; }
        public IRelayCommand BuscarCommand { get; }
        public IRelayCommand<bool> GenerarConstanciaCommand { get; }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService();
            ResultadosBusqueda = new ObservableCollection<Documento>();

            CargarPdfCommand = new RelayCommand(CargarPdf);
            BuscarCommand = new RelayCommand(Buscar);
            GenerarConstanciaCommand = new RelayCommand<bool>(GenerarConstancia);
        }

        private void CargarPdf()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos PDF (.pdf)|*.pdf",
                Title = "Seleccionar archivo PDF para cargar"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string carpetaDestino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados");
                    Directory.CreateDirectory(carpetaDestino);
                    string nombreArchivo = Path.GetFileName(openFileDialog.FileName);
                    string rutaDestino = Path.Combine(carpetaDestino, nombreArchivo);
                    File.Copy(openFileDialog.FileName, rutaDestino, true);
                    string contenido = _pdfService.ExtraerTextoDePdf(rutaDestino);
                    _databaseService.GuardarDocumento(nombreArchivo, rutaDestino, contenido);
                    MessageBox.Show("¡PDF cargado y procesado exitosamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al cargar el PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                MessageBox.Show("Por favor, ingrese un término de búsqueda.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var resultados = _databaseService.BuscarTermino(TerminoBusqueda);
                ResultadosBusqueda.Clear();
                PdfContent = string.Empty; // Limpiar el contenido al buscar de nuevo
                foreach (var doc in resultados)
                {
                    ResultadosBusqueda.Add(doc);
                }
                _databaseService.RegistrarBusqueda(TerminoBusqueda, resultados.Count > 0);
                if (resultados.Count == 0)
                {
                    MessageBox.Show("No se encontraron coincidencias para el término buscado.", "Búsqueda Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al realizar la búsqueda: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerarConstancia(bool esAprobatoria)
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                MessageBox.Show("Debe realizar una búsqueda primero para generar una constancia.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Crear un nombre de archivo temporal para la constancia
                string tempFileName = $"Constancia_{TerminoBusqueda.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                // 2. Usar el servicio para crear el PDF en la ruta temporal
                _pdfService.GenerarConstancia(tempFilePath, TerminoBusqueda, esAprobatoria);

                // 3. Abrir nuestra nueva ventana de visor con la ruta del archivo temporal
                var viewer = new PdfViewerWindow(tempFilePath);
                viewer.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar la constancia: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPdfContent(int docId)
        {
            try
            {
                PdfContent = _databaseService.GetDocumentoContent(docId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el contenido del documento: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                PdfContent = "Error al cargar el contenido del documento.";
            }
        }
    }
}