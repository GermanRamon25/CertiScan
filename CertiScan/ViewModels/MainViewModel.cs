using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CertiScan.Services;
using System;
using System.IO; // Asegúrate de que este 'using' esté presente

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
            set
            {
                if (SetProperty(ref _terminoBusqueda, value) && string.IsNullOrWhiteSpace(value))
                {
                    ClearSearchHighlights();
                }
            }
        }

        public ObservableCollection<DocumentoViewModel> DocumentosMostrados { get; set; }

        private DocumentoViewModel _selectedDocumento;
        public DocumentoViewModel SelectedDocumento
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

        private FlowDocument _contenidoDocumento;
        public FlowDocument ContenidoDocumento
        {
            get => _contenidoDocumento;
            set => SetProperty(ref _contenidoDocumento, value);
        }

        public IRelayCommand CargarPdfCommand { get; }
        public IRelayCommand BuscarCommand { get; }
        public IRelayCommand<bool> GenerarConstanciaCommand { get; }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService();
            DocumentosMostrados = new ObservableCollection<DocumentoViewModel>();

            LoadAllDocuments();

            CargarPdfCommand = new RelayCommand(CargarPdf);
            BuscarCommand = new RelayCommand(Buscar);
            GenerarConstanciaCommand = new RelayCommand<bool>(GenerarConstancia);
        }

        private void LoadAllDocuments()
        {
            DocumentosMostrados.Clear();
            var documentos = _databaseService.GetAllDocuments();
            foreach (var doc in documentos)
            {
                DocumentosMostrados.Add(new DocumentoViewModel(doc));
            }
        }

        private void CargarPdf()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
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

                    LoadAllDocuments();
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
                ClearSearchHighlights();
                MessageBox.Show("Por favor, ingrese un término de búsqueda.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var resultados = _databaseService.BuscarTermino(TerminoBusqueda);
                var resultadoIds = new HashSet<int>(resultados.Select(r => r.Id));

                foreach (var docVm in DocumentosMostrados)
                {
                    docVm.IsSearchResult = resultadoIds.Contains(docVm.Id);
                }

                _databaseService.RegistrarBusqueda(TerminoBusqueda, resultados.Count > 0);
                if (resultados.Count == 0)
                {
                    MessageBox.Show("No se encontraron coincidencias.", "Búsqueda Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al realizar la búsqueda: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearSearchHighlights()
        {
            foreach (var docVm in DocumentosMostrados)
            {
                docVm.IsSearchResult = false;
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
                string tempFileName = $"Constancia_{TerminoBusqueda.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                _pdfService.GenerarConstancia(tempFilePath, TerminoBusqueda, esAprobatoria);

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
                string plainText = _databaseService.GetDocumentoContent(docId);
                ContenidoDocumento = CreateHighlightedFlowDocument(plainText, TerminoBusqueda);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el contenido: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateHighlightedFlowDocument(string text, string searchTerm)
        {
            var flowDocument = new FlowDocument();
            var paragraph = new Paragraph();

            if (string.IsNullOrEmpty(text))
            {
                flowDocument.Blocks.Add(new Paragraph(new Run("El documento está vacío o no se pudo leer el contenido.")));
                return flowDocument;
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                paragraph.Inlines.Add(new Run(text));
                flowDocument.Blocks.Add(paragraph);
                return flowDocument;
            }

            int currentIndex = 0;
            int searchTermIndex;
            while ((searchTermIndex = text.IndexOf(searchTerm, currentIndex, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                if (searchTermIndex > currentIndex)
                {
                    paragraph.Inlines.Add(new Run(text.Substring(currentIndex, searchTermIndex - currentIndex)));
                }

                var highlightedRun = new Run(text.Substring(searchTermIndex, searchTerm.Length))
                {
                    Background = Brushes.Yellow,
                    FontWeight = FontWeights.Bold
                };
                paragraph.Inlines.Add(highlightedRun);

                currentIndex = searchTermIndex + searchTerm.Length;
            }

            if (currentIndex < text.Length)
            {
                paragraph.Inlines.Add(new Run(text.Substring(currentIndex)));
            }

            flowDocument.Blocks.Add(paragraph);
            return flowDocument;
        }
    }
}