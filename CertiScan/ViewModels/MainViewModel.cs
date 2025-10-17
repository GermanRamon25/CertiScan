using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CertiScan.Services; // Asegúrate de que este using esté presente
using System;
using System.IO;
using System.Text.RegularExpressions; // Necesario para expresiones regulares

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
                if (SetProperty(ref _terminoBusqueda, value))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        ClearSearchHighlights();
                        if (SelectedDocumento != null)
                            LoadPdfContent(SelectedDocumento.Id); // Recargar sin resaltado
                    }
                    // Si hay término y documento seleccionado, recargar con resaltado
                    else if (SelectedDocumento != null)
                    {
                        LoadPdfContent(SelectedDocumento.Id);
                    }
                }
            }
        }

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
        public IRelayCommand ShowHistoryCommand { get; }
        public IRelayCommand RefreshCommand { get; }

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
            ShowHistoryCommand = new RelayCommand(ShowHistory);
            RefreshCommand = new RelayCommand(RefreshView);

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
                    SelectedDocumento = null; // Limpiar selección

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

                    RefreshView(); // Refrescar vista después de cargar
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
                ResultadoEncontrado = false;
                UpdateConstanciaButtonStates();
                if (SelectedDocumento != null)
                    LoadPdfContent(SelectedDocumento.Id); // Recargar sin resaltado
                MessageBox.Show("Por favor, ingrese un término de búsqueda.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var resultados = _databaseService.BuscarTermino(TerminoBusqueda);
                bool encontrado = resultados.Count > 0;

                ResultadoEncontrado = encontrado;
                UpdateConstanciaButtonStates();

                var resultadoIds = new HashSet<int>(resultados.Select(r => r.Id));

                foreach (var docVm in DocumentosMostrados)
                {
                    docVm.IsSearchResult = resultadoIds.Contains(docVm.Id);
                }

                if (SelectedDocumento != null)
                {
                    LoadPdfContent(SelectedDocumento.Id); // Recargar CON resaltado
                }

                _databaseService.RegistrarBusqueda(TerminoBusqueda, encontrado, SessionService.CurrentUserId);

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

        private void UpdateConstanciaButtonStates()
        {
            bool canGenerate = !string.IsNullOrWhiteSpace(TerminoBusqueda);

            if (ResultadoEncontrado)
            {
                IsAprobatoriaButtonEnabled = false;
                IsDenegadaButtonEnabled = canGenerate;
            }
            else
            {
                IsAprobatoriaButtonEnabled = canGenerate;
                IsDenegadaButtonEnabled = false;
            }
        }

        private void ClearSearchHighlights()
        {
            foreach (var docVm in DocumentosMostrados)
            {
                docVm.IsSearchResult = false;
            }
        }

        private void GenerarConstancia(bool ignoredParameter)
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                MessageBox.Show("Debe realizar una búsqueda primero para generar una constancia.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                ContenidoDocumento = new FlowDocument();
            }
        }

        private void ShowHistory()
        {
            var historyWindow = new HistoryWindow();
            historyWindow.Show();
        }

        private void RefreshView()
        {
            TerminoBusqueda = string.Empty; // Dispara la lógica en el setter para quitar resaltados
            SelectedDocumento = null;
            ClearSearchHighlights();
            LoadAllDocuments();
            ResultadoEncontrado = false;
            UpdateConstanciaButtonStates();
            ContenidoDocumento = new FlowDocument();
        }

        // --- MÉTODO ACTUALIZADO ---
        private FlowDocument CreateHighlightedFlowDocument(string text, string searchTerm)
        {
            var flowDocument = new FlowDocument();

            if (string.IsNullOrEmpty(text))
            {
                flowDocument.Blocks.Add(new Paragraph(new Run("El documento está vacío o no se pudo leer el contenido.")));
                return flowDocument;
            }

            // Normalizar saltos de línea y reducir múltiples a dos máximo
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            // Dividir en bloques principales (separados por doble salto de línea)
            string[] blocks = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            bool applyHighlight = !string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Length >= 2;

            foreach (string blockText in blocks)
            {
                // Crear un párrafo para el bloque completo
                var blockParagraph = new Paragraph
                {
                    Margin = new Thickness(0, 0, 0, 15) // Espacio entre bloques
                };

                // Resaltado de fondo para todo el bloque si contiene el término
                bool blockContainsSearchTerm = applyHighlight && blockText.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                if (blockContainsSearchTerm)
                {
                    blockParagraph.Background = new SolidColorBrush(Color.FromRgb(255, 247, 225)); // #FFF7E1
                }

                // Dividir el bloque en líneas individuales para aplicar formato específico
                string[] lines = blockText.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Añadir salto de línea si no es la primera línea del bloque
                    if (i > 0)
                    {
                        blockParagraph.Inlines.Add(new LineBreak());
                    }

                    // --- Lógica de formato específico por línea ---
                    bool isHandled = false; // Flag para saber si ya se aplicó un formato especial

                    // Nombres principales
                    if (line.StartsWith("'ABD AL-MALIK") || line.StartsWith("'ABD AL-RAHMAN"))
                    {
                        AddFormattedRun(blockParagraph, "NOMBRE: ", Brushes.Gray, FontWeights.Bold);
                        AddHighlightedTextToParagraph(blockParagraph, line, searchTerm, FontWeights.Bold);
                        isHandled = true;
                    }
                    // Alias principal
                    else if (line.StartsWith("también conocido como"))
                    {
                        AddFormattedRun(blockParagraph, "Alias: ", Brushes.DarkGray, FontWeights.SemiBold);
                        AddHighlightedTextToParagraph(blockParagraph, line.Substring("también conocido como".Length).Trim(), searchTerm);
                        isHandled = true;
                    }
                    // Enlace Interpol
                    else if (line.StartsWith("Liga de identificación Interpol:"))
                    {
                        AddFormattedRun(blockParagraph, "Enlace Interpol: ", Brushes.DarkGray, FontWeights.SemiBold);
                        string url = line.Substring("Liga de identificación Interpol:".Length).Trim();
                        if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
                        {
                            var hyperlink = new Hyperlink { NavigateUri = uriResult, Foreground = Brushes.Blue, TextDecorations = TextDecorations.Underline };
                            hyperlink.RequestNavigate += (sender, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                            foreach (var inline in CreateHighlightedRuns(url, searchTerm)) { hyperlink.Inlines.Add(inline); }
                            blockParagraph.Inlines.Add(hyperlink);
                        }
                        else
                        {
                            // Si la URL está vacía o no es válida, mostrarla como texto normal
                            AddHighlightedTextToParagraph(blockParagraph, url, searchTerm);
                        }
                        isHandled = true;
                    }
                    // Fechas de Nacimiento (heurística)
                    else if (Regex.IsMatch(line, @"\d{1,2} de \w+ de \d{4}$|\d{4}$|^\w\)\s*\d{4}$"))
                    {
                        AddFormattedRun(blockParagraph, "Fecha Nacimiento: ", Brushes.DarkGray, FontWeights.SemiBold);
                        AddHighlightedTextToParagraph(blockParagraph, line, searchTerm);
                        isHandled = true;
                    }
                    // Alias secundarios (a), b), c)...) - Añadir indentación
                    else if (Regex.IsMatch(line, @"^\s*[a-z]\)\s+"))
                    {
                        blockParagraph.Inlines.Add(new Run("    ")); // Indentación
                        AddHighlightedTextToParagraph(blockParagraph, line.TrimStart(), searchTerm);
                        isHandled = true;
                    }

                    // Líneas genéricas (si no coincidió con ningún patrón especial)
                    if (!isHandled)
                    {
                        // Poner en negrita las etiquetas comunes si aparecen solas en una línea
                        if (line.Equals("NOMBRE", StringComparison.OrdinalIgnoreCase) ||
                            line.Equals("FECHA DE NACIMIENTO", StringComparison.OrdinalIgnoreCase))
                        {
                            AddHighlightedTextToParagraph(blockParagraph, line, searchTerm, FontWeights.Bold, Brushes.Gray);
                        }
                        else
                        {
                            AddHighlightedTextToParagraph(blockParagraph, line, searchTerm);
                        }
                    }
                }
                flowDocument.Blocks.Add(blockParagraph);
            }

            return flowDocument;
        }

        // --- Métodos Helper (sin cambios respecto a la versión anterior) ---
        private void AddFormattedRun(Paragraph paragraph, string text, Brush foreground, FontWeight fontWeight)
        {
            paragraph.Inlines.Add(new Run(text) { Foreground = foreground, FontWeight = fontWeight });
        }
        private void AddHighlightedTextToParagraph(Paragraph paragraph, string text, string searchTerm)
        {
            AddHighlightedTextToParagraph(paragraph, text, searchTerm, FontWeights.Normal, null);
        }
        private void AddHighlightedTextToParagraph(Paragraph paragraph, string text, string searchTerm, FontWeight defaultWeight)
        {
            AddHighlightedTextToParagraph(paragraph, text, searchTerm, defaultWeight, null);
        }
        private void AddHighlightedTextToParagraph(Paragraph paragraph, string text, string searchTerm, FontWeight defaultWeight, Brush defaultForeground)
        {
            foreach (var inline in CreateHighlightedRuns(text, searchTerm, defaultWeight, defaultForeground))
            {
                paragraph.Inlines.Add(inline);
            }
        }
        private List<Inline> CreateHighlightedRuns(string text, string searchTerm)
        {
            return CreateHighlightedRuns(text, searchTerm, FontWeights.Normal, null);
        }
        private List<Inline> CreateHighlightedRuns(string text, string searchTerm, FontWeight defaultWeight)
        {
            return CreateHighlightedRuns(text, searchTerm, defaultWeight, null);
        }
        private List<Inline> CreateHighlightedRuns(string text, string searchTerm, FontWeight defaultWeight, Brush defaultForeground)
        {
            var inlines = new List<Inline>();
            if (string.IsNullOrWhiteSpace(text)) return inlines;
            bool applyWordHighlight = !string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Length >= 2;

            if (!applyWordHighlight)
            {
                inlines.Add(new Run(text) { FontWeight = defaultWeight, Foreground = defaultForeground ?? Brushes.Black });
                return inlines;
            }

            int currentIndex = 0;
            int searchTermIndex;
            while ((searchTermIndex = text.IndexOf(searchTerm, currentIndex, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                if (searchTermIndex > currentIndex)
                {
                    inlines.Add(new Run(text.Substring(currentIndex, searchTermIndex - currentIndex)) { FontWeight = defaultWeight, Foreground = defaultForeground ?? Brushes.Black });
                }
                var highlightedRun = new Run(text.Substring(searchTermIndex, searchTerm.Length))
                {
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)), // Fondo oscuro
                    Foreground = Brushes.Yellow,                                 // Texto amarillo
                    FontWeight = FontWeights.ExtraBold                           // Negrita extra
                };
                inlines.Add(highlightedRun);
                currentIndex = searchTermIndex + searchTerm.Length;
            }
            if (currentIndex < text.Length)
            {
                inlines.Add(new Run(text.Substring(currentIndex)) { FontWeight = defaultWeight, Foreground = defaultForeground ?? Brushes.Black });
            }
            return inlines;
        }
    }
}