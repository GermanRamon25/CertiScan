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
using System.Text.RegularExpressions;
using System.Diagnostics;
using CertiScan.Models; // Necesario para Documento
using System.Text; // Necesario para StringBuilder

namespace CertiScan.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;

        public string NombreUsuarioLogueado { get; }

        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda
        {
            get => _terminoBusqueda;
            set
            {
                if (SetProperty(ref _terminoBusqueda, value))
                {
                    _nombresArchivosEncontrados.Clear();
                    ResultadoEncontrado = false;
                    UpdateConstanciaButtonStates();

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        ClearSearchHighlights();
                        if (SelectedDocumento != null)
                            LoadPdfContent(SelectedDocumento.Id);
                    }
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
            set
            {
                if (SetProperty(ref _resultadoEncontrado, value))
                {
                    UpdateConstanciaButtonStates();
                }
            }
        }

        private List<string> _nombresArchivosEncontrados = new List<string>();

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
            CargarPdfCommand = new RelayCommand(CargarPdf); // <- Llamará al método modificado
            BuscarCommand = new RelayCommand(Buscar);
            GenerarConstanciaCommand = new RelayCommand<bool>(GenerarConstancia);
            DeletePdfCommand = new RelayCommand(DeletePdf, CanDeletePdf);
            ShowHistoryCommand = new RelayCommand(ShowHistory);
            RefreshCommand = new RelayCommand(RefreshView);

            NombreUsuarioLogueado = SessionService.CurrentUserName;
            UpdateConstanciaButtonStates();
        }

        private bool CanDeletePdf()
        {
            return SelectedDocumento != null;
        }

        private void DeletePdf()
        {
            if (!CanDeletePdf()) return;
            var result = MessageBox.Show($"¿Estás seguro de que quieres eliminar permanentemente el archivo '{SelectedDocumento.NombreArchivo}'?\n\nEsta acción no se puede deshacer.", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string filePath = _databaseService.DeleteDocument(SelectedDocumento.Id);
                    if (File.Exists(filePath)) File.Delete(filePath);
                    DocumentosMostrados.Remove(SelectedDocumento);
                    SelectedDocumento = null;
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
            foreach (var doc in documentos) DocumentosMostrados.Add(new DocumentoViewModel(doc));
        }


        // --- INICIO DE LA MODIFICACIÓN: Cargar Múltiples PDFs ---
        private void CargarPdf()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos PDF (.pdf)|*.pdf",
                Title = "Seleccionar archivo(s) PDF para cargar",
                Multiselect = true // <<< AÑADIDO: Permite seleccionar múltiples archivos
            };

            if (openFileDialog.ShowDialog() == true)
            {
                int archivosCargados = 0;
                int archivosFallidos = 0;
                StringBuilder errores = new StringBuilder();

                // Usar un bucle para procesar cada archivo seleccionado
                foreach (string rutaArchivoOriginal in openFileDialog.FileNames) // <<< CAMBIADO: Usa FileNames (plural)
                {
                    try
                    {
                        string carpetaDestino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados");
                        Directory.CreateDirectory(carpetaDestino); // Crea la carpeta si no existe

                        string nombreArchivo = Path.GetFileName(rutaArchivoOriginal);
                        string rutaDestino = Path.Combine(carpetaDestino, nombreArchivo);

                        // Copiar el archivo (sobrescribir si ya existe)
                        File.Copy(rutaArchivoOriginal, rutaDestino, true);

                        // Extraer texto
                        string contenido = _pdfService.ExtraerTextoDePdf(rutaDestino);

                        // Guardar en la base de datos
                        _databaseService.GuardarDocumento(nombreArchivo, rutaDestino, contenido);

                        archivosCargados++;
                    }
                    catch (Exception ex)
                    {
                        // Registrar el error para mostrarlo al final
                        archivosFallidos++;
                        errores.AppendLine($"- Error al procesar '{Path.GetFileName(rutaArchivoOriginal)}': {ex.Message}");
                    }
                } // Fin del bucle foreach

                // Mensaje final al usuario
                StringBuilder mensajeFinal = new StringBuilder();
                mensajeFinal.AppendLine($"Proceso completado.");
                mensajeFinal.AppendLine($"Archivos cargados exitosamente: {archivosCargados}");
                if (archivosFallidos > 0)
                {
                    mensajeFinal.AppendLine($"Archivos con error: {archivosFallidos}");
                    mensajeFinal.AppendLine("\nDetalles de errores:");
                    mensajeFinal.Append(errores.ToString());
                    MessageBox.Show(mensajeFinal.ToString(), "Resultado de la Carga (con errores)", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(mensajeFinal.ToString(), "Resultado de la Carga", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Refrescar la vista para mostrar los nuevos archivos
                RefreshView();
            }
        }
        // --- FIN DE LA MODIFICACIÓN ---


        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                ClearSearchHighlights();
                ResultadoEncontrado = false;
                _nombresArchivosEncontrados.Clear();
                if (SelectedDocumento != null) LoadPdfContent(SelectedDocumento.Id);
                MessageBox.Show("Por favor, ingrese un término de búsqueda.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                List<Documento> resultados = _databaseService.BuscarTermino(TerminoBusqueda);
                bool encontrado = resultados.Count > 0;
                _nombresArchivosEncontrados = encontrado ? resultados.Select(d => d.NombreArchivo).ToList() : new List<string>();
                ResultadoEncontrado = encontrado;
                var resultadoIds = new HashSet<int>(resultados.Select(r => r.Id));
                foreach (var docVm in DocumentosMostrados) docVm.IsSearchResult = resultadoIds.Contains(docVm.Id);
                if (SelectedDocumento != null) LoadPdfContent(SelectedDocumento.Id);
                _databaseService.RegistrarBusqueda(TerminoBusqueda, encontrado, SessionService.CurrentUserId);
                if (encontrado) MessageBox.Show("¡Coincidencia(s) encontrada(s)!", "Búsqueda Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                else MessageBox.Show("No se encontraron coincidencias.", "Búsqueda Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al realizar la búsqueda: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ResultadoEncontrado = false;
                _nombresArchivosEncontrados.Clear();
            }
        }


        private void UpdateConstanciaButtonStates()
        {
            bool canGenerate = !string.IsNullOrWhiteSpace(TerminoBusqueda);
            IsAprobatoriaButtonEnabled = canGenerate && !ResultadoEncontrado;
            IsDenegadaButtonEnabled = canGenerate && ResultadoEncontrado;
        }


        private void ClearSearchHighlights()
        {
            foreach (var docVm in DocumentosMostrados) docVm.IsSearchResult = false;
        }


        

        private void GenerarConstancia(bool generarAprobatoriaParametroIgnorado)
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                MessageBox.Show("Debe realizar una búsqueda primero para generar una constancia.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            bool esAprobatoriaReal = !ResultadoEncontrado;
            try
            {
                string tempFileName = $"Constancia_{TerminoBusqueda.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                _pdfService.GenerarConstancia(tempFilePath, TerminoBusqueda, esAprobatoriaReal, _nombresArchivosEncontrados);
                var viewer = new PdfViewerWindow(tempFilePath);
                viewer.Show();

                // 🟢 INICIO DE LA CORRECCIÓN: Limpiar la búsqueda y refrescar la vista.
                RefreshView();
                // 🟢 FIN DE LA CORRECCIÓN

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
            var historyWindow = new HistoryWindow(); historyWindow.Show();
        }


        private void RefreshView()
        {
            TerminoBusqueda = string.Empty;
            SelectedDocumento = null;
            ClearSearchHighlights();
            LoadAllDocuments();
            ContenidoDocumento = new FlowDocument();
        }


        // --- CreateHighlightedFlowDocument y Helpers (Sin cambios) ---
        private FlowDocument CreateHighlightedFlowDocument(string text, string searchTerm)
        {
            var flowDocument = new FlowDocument();
            if (string.IsNullOrEmpty(text)) { flowDocument.Blocks.Add(new Paragraph(new Run("El documento está vacío o no se pudo leer el contenido."))); return flowDocument; }
            text = text.Replace("\r\n", "\n").Replace("\r", "\n"); text = Regex.Replace(text, @"\n{3,}", "\n\n");
            string[] blocks = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            bool applyHighlight = !string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Length >= 2;
            var dateRegex = new Regex(@"(Fecha de Nacimiento:|Nacimiento:)\s*(?!http|www)(.*)", RegexOptions.IgnoreCase);
            foreach (string blockText in blocks)
            {
                var blockParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, 15) };
                bool blockContainsSearchTerm = applyHighlight && blockText.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                if (blockContainsSearchTerm) blockParagraph.Background = new SolidColorBrush(Color.FromRgb(255, 247, 225));
                string[] lines = blockText.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim(); if (string.IsNullOrWhiteSpace(line)) continue;
                    if (i > 0) blockParagraph.Inlines.Add(new LineBreak());
                    bool isHandled = false;
                    if (line.StartsWith("Liga de identificación Interpol:", StringComparison.OrdinalIgnoreCase))
                    {
                        AddFormattedRun(blockParagraph, "Enlace Interpol: ", Brushes.DarkGray, FontWeights.SemiBold); string url = line.Substring("Liga de identificación Interpol:".Length).Trim();
                        if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)) { var hyperlink = new Hyperlink { NavigateUri = uriResult, Foreground = Brushes.Blue, TextDecorations = TextDecorations.Underline }; hyperlink.RequestNavigate += (sender, e) => Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); foreach (var inline in CreateHighlightedRuns(url, searchTerm)) { hyperlink.Inlines.Add(inline); } blockParagraph.Inlines.Add(hyperlink); } else { AddHighlightedTextToParagraph(blockParagraph, url, searchTerm); }
                        isHandled = true;
                    }
                    else if (line.StartsWith("'ABD AL-MALIK") || line.StartsWith("'ABD AL-RAHMAN")) { AddFormattedRun(blockParagraph, "NOMBRE: ", Brushes.Gray, FontWeights.Bold); AddHighlightedTextToParagraph(blockParagraph, line, searchTerm, FontWeights.Bold); isHandled = true; } else if (line.StartsWith("también conocido como", StringComparison.OrdinalIgnoreCase)) { AddFormattedRun(blockParagraph, "Alias: ", Brushes.DarkGray, FontWeights.SemiBold); AddHighlightedTextToParagraph(blockParagraph, line.Substring("también conocido como".Length).Trim(), searchTerm); isHandled = true; } else { var dateMatch = dateRegex.Match(line); if (dateMatch.Success) { string prefix = dateMatch.Groups[1].Value; string dateText = dateMatch.Groups[2].Value.Trim(); if (!string.IsNullOrWhiteSpace(dateText)) { AddFormattedRun(blockParagraph, prefix.Trim() + ": ", Brushes.DarkGray, FontWeights.SemiBold); AddHighlightedTextToParagraph(blockParagraph, dateText, searchTerm, FontWeights.Bold, Brushes.Black); isHandled = true; } } }
                    if (!isHandled && Regex.IsMatch(line, @"^\s*[a-z]\)\s+")) { blockParagraph.Inlines.Add(new Run("    ")); AddHighlightedTextToParagraph(blockParagraph, line.TrimStart(), searchTerm); isHandled = true; }
                    if (!isHandled) { if (line.Equals("NOMBRE", StringComparison.OrdinalIgnoreCase) || line.Equals("FECHA DE NACIMIENTO", StringComparison.OrdinalIgnoreCase)) AddHighlightedTextToParagraph(blockParagraph, line, searchTerm, FontWeights.Bold, Brushes.Gray); else AddHighlightedTextToParagraph(blockParagraph, line, searchTerm); }
                }
                flowDocument.Blocks.Add(blockParagraph);
            }
            return flowDocument;
        }

        private void AddFormattedRun(Paragraph paragraph, string text, Brush foreground, FontWeight fontWeight) { paragraph.Inlines.Add(new Run(text) { Foreground = foreground, FontWeight = fontWeight }); }
        private void AddHighlightedTextToParagraph(Paragraph paragraph, string text, string searchTerm) { AddHighlightedTextToParagraph(paragraph, text, searchTerm, FontWeights.Normal, null); }
        private void AddHighlightedTextToParagraph(Paragraph paragraph, string text, string searchTerm, FontWeight defaultWeight) { AddHighlightedTextToParagraph(paragraph, text, searchTerm, defaultWeight, null); }
        private void AddHighlightedTextToParagraph(Paragraph paragraph, string text, string searchTerm, FontWeight defaultWeight, Brush defaultForeground) { foreach (var inline in CreateHighlightedRuns(text, searchTerm, defaultWeight, defaultForeground)) paragraph.Inlines.Add(inline); }
        private List<Inline> CreateHighlightedRuns(string text, string searchTerm) { return CreateHighlightedRuns(text, searchTerm, FontWeights.Normal, null); }
        private List<Inline> CreateHighlightedRuns(string text, string searchTerm, FontWeight defaultWeight) { return CreateHighlightedRuns(text, searchTerm, defaultWeight, null); }
        private List<Inline> CreateHighlightedRuns(string text, string searchTerm, FontWeight defaultWeight, Brush defaultForeground)
        {
            var inlines = new List<Inline>(); if (string.IsNullOrWhiteSpace(text)) return inlines; bool applyWordHighlight = !string.IsNullOrWhiteSpace(searchTerm) && searchTerm.Length >= 2;
            if (!applyWordHighlight) { inlines.Add(new Run(text) { FontWeight = defaultWeight, Foreground = defaultForeground ?? Brushes.Black }); return inlines; }
            int currentIndex = 0; int searchTermIndex;
            while ((searchTermIndex = text.IndexOf(searchTerm, currentIndex, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                if (searchTermIndex > currentIndex) inlines.Add(new Run(text.Substring(currentIndex, searchTermIndex - currentIndex)) { FontWeight = defaultWeight, Foreground = defaultForeground ?? Brushes.Black });
                var highlightedRun = new Run(text.Substring(searchTermIndex, searchTerm.Length)) { Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)), Foreground = Brushes.Yellow, FontWeight = FontWeights.ExtraBold }; inlines.Add(highlightedRun); currentIndex = searchTermIndex + searchTerm.Length;
            }
            if (currentIndex < text.Length) inlines.Add(new Run(text.Substring(currentIndex)) { FontWeight = defaultWeight, Foreground = defaultForeground ?? Brushes.Black }); return inlines;
        }

    }
}