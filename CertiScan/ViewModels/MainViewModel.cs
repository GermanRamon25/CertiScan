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
using System.IO;

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

        // PROPIEDADES DE ESTADO CLAVE
        private bool _resultadoEncontrado = false;
        public bool ResultadoEncontrado
        {
            get => _resultadoEncontrado;
            set => SetProperty(ref _resultadoEncontrado, value);
        }

        private bool _isAprobatoriaButtonEnabled = true;
        public bool IsAprobatoriaButtonEnabled
        {
            get => _isAprobatoriaButtonEnabled;
            set => SetProperty(ref _isAprobatoriaButtonEnabled, value);
        }

        private bool _isDenegadaButtonEnabled = true;
        public bool IsDenegadaButtonEnabled
        {
            get => _isDenegadaButtonEnabled;
            set => SetProperty(ref _isDenegadaButtonEnabled, value);
        }

        public ObservableCollection<DocumentoViewModel> DocumentosMostrados { get; set; }

        private DocumentoViewModel _selectedDocumento;
        public DocumentoViewModel SelectedDocumento
        {
            get => _selectedDocumento;
            set
            {
                if (SetProperty(ref _selectedDocumento, value))
                {
                    if (value != null)
                    {
                        LoadPdfContent(value.Id);
                    }
                    else
                    {
                        ContenidoDocumento = new FlowDocument();
                    }
                }
                DeletePdfCommand.NotifyCanExecuteChanged();
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
        public IRelayCommand DeletePdfCommand { get; }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService();
            DocumentosMostrados = new ObservableCollection<DocumentoViewModel>();
            LoadAllDocuments();
            CargarPdfCommand = new RelayCommand(CargarPdf);
            BuscarCommand = new RelayCommand(Buscar);
            GenerarConstanciaCommand = new RelayCommand<bool>(GenerarConstancia);
            DeletePdfCommand = new RelayCommand(DeletePdf, CanDeletePdf);

            // Establece el estado inicial: Aprobatoria habilitada.
            UpdateConstanciaButtonStates();
        }

        private bool CanDeletePdf()
        {
            return SelectedDocumento != null;
        }

        private void DeletePdf()
        {
            if (!CanDeletePdf()) return;

            var result = MessageBox.Show($"¿Estás seguro de que quieres eliminar permanentemente el archivo '{SelectedDocumento.NombreArchivo}'?\n\nEsta acción no se puede deshacer.",
                                         "Confirmar Eliminación",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string filePath = _databaseService.DeleteDocument(SelectedDocumento.Id);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    DocumentosMostrados.Remove(SelectedDocumento);

                    ContenidoDocumento = new FlowDocument();

                    MessageBox.Show("Documento eliminado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al eliminar el documento: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

        // MÉTODO CLAVE: Contiene la lógica para el resaltado y la habilitación
        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                ClearSearchHighlights();
                ResultadoEncontrado = false;
                UpdateConstanciaButtonStates();
                MessageBox.Show("Por favor, ingrese un término de búsqueda.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var resultados = _databaseService.BuscarTermino(TerminoBusqueda);
                bool encontrado = resultados.Count > 0;

                ResultadoEncontrado = encontrado; // ASIGNACIÓN CLAVE
                UpdateConstanciaButtonStates(); // LLAMADA CLAVE: Bloquea el botón incorrecto

                var resultadoIds = new HashSet<int>(resultados.Select(r => r.Id));

                foreach (var docVm in DocumentosMostrados)
                {
                    docVm.IsSearchResult = resultadoIds.Contains(docVm.Id);
                }

                // FORZAR RECARGA DE CONTENIDO: Permite buscar y resaltar inmediatamente 
                // en el mismo archivo sin hacer clic de nuevo.
                if (SelectedDocumento != null)
                {
                    LoadPdfContent(SelectedDocumento.Id);
                }

                _databaseService.RegistrarBusqueda(TerminoBusqueda, encontrado);

                if (encontrado)
                {
                    MessageBox.Show("¡Coincidencia(s) encontrada(s)!", "Búsqueda Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No se encontraron coincidencias.", "Búsqueda Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al realizar la búsqueda: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // MÉTODO CLAVE: Habilita/Deshabilita los botones
        private void UpdateConstanciaButtonStates()
        {
            // Si la persona fue ENCONTRADA (lista negra), la ÚNICA opción válida es DENEGADA.
            if (ResultadoEncontrado)
            {
                IsAprobatoriaButtonEnabled = false; // Deshabilitar Aprobatoria
                IsDenegadaButtonEnabled = true;     // Habilitar Denegada
            }
            // Si la persona NO fue ENCONTRADA, la ÚNICA opción válida es APROBATORIA.
            else
            {
                IsAprobatoriaButtonEnabled = true;  // Habilitar Aprobatoria
                IsDenegadaButtonEnabled = false;    // Deshabilitar Denegada
            }
        }

        private void ClearSearchHighlights()
        {
            foreach (var docVm in DocumentosMostrados)
            {
                docVm.IsSearchResult = false;
            }
        }

        // GenerarConstancia siempre forzará el resultado correcto basado en el estado.
        private void GenerarConstancia(bool ignoredParameter)
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                MessageBox.Show("Debe realizar una búsqueda primero para generar una constancia.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Forzamos el resultado: Si se encontró, la constancia NO es Aprobatoria. Si no, SÍ es Aprobatoria.
            bool generarAprobatoria = !ResultadoEncontrado;

            try
            {
                string tempFileName = $"Constancia_{TerminoBusqueda.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                _pdfService.GenerarConstancia(tempFilePath, TerminoBusqueda, generarAprobatoria);

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

            if (string.IsNullOrEmpty(text))
            {
                flowDocument.Blocks.Add(new Paragraph(new Run("El documento está vacío o no se pudo leer el contenido.")));
                return flowDocument;
            }

            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                string[] paragraphs = text.Split(new[] { "\n\n" }, StringSplitOptions.None);
                foreach (string p in paragraphs)
                {
                    flowDocument.Blocks.Add(new Paragraph(new Run(p)));
                }
                return flowDocument;
            }

            // Dividir el texto en líneas para procesarlas individualmente.
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    flowDocument.Blocks.Add(new Paragraph(new Run("")));
                    continue;
                }

                var paragraph = new Paragraph();
                string currentLine = line;
                int currentIndex = 0;
                int searchTermIndex;

                while ((searchTermIndex = currentLine.IndexOf(searchTerm, currentIndex, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    if (searchTermIndex > currentIndex)
                    {
                        paragraph.Inlines.Add(new Run(currentLine.Substring(currentIndex, searchTermIndex - currentIndex)));
                    }

                    // Resaltado de ALTO CONTRASTE 
                    var highlightedRun = new Run(currentLine.Substring(searchTermIndex, searchTerm.Length))
                    {
                        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        Foreground = Brushes.Yellow,
                        FontWeight = FontWeights.ExtraBold
                    };
                    paragraph.Inlines.Add(highlightedRun);

                    currentIndex = searchTermIndex + searchTerm.Length;
                }

                if (currentIndex < currentLine.Length)
                {
                    paragraph.Inlines.Add(new Run(currentLine.Substring(currentIndex)));
                }

                flowDocument.Blocks.Add(paragraph);
            }

            return flowDocument;
        }
    }
}