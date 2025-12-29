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
using CertiScan.Models;
using System.Text;

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

        // --- PROPIEDADES PARA DATOS MANUALES ---
        private string _nombreNotarioInput;
        public string NombreNotarioInput { get => _nombreNotarioInput; set => SetProperty(ref _nombreNotarioInput, value); }

        private string _numeroNotariaInput;
        public string NumeroNotariaInput { get => _numeroNotariaInput; set => SetProperty(ref _numeroNotariaInput, value); }

        private string _direccionInput;
        public string DireccionInput { get => _direccionInput; set => SetProperty(ref _direccionInput, value); }

        private string _contactoInput;
        public string ContactoInput { get => _contactoInput; set => SetProperty(ref _contactoInput, value); }
        // ---------------------------------------

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
                    if (value != null) LoadPdfContent(value.Id);
                    else ContenidoDocumento = new FlowDocument();
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

            NombreUsuarioLogueado = SessionService.CurrentUserName;
            UpdateConstanciaButtonStates();
        }

        private bool CanDeletePdf() => SelectedDocumento != null;

        private void DeletePdf()
        {
            if (!CanDeletePdf()) return;
            var result = MessageBox.Show($"¿Estás seguro de que quieres eliminar '{SelectedDocumento.NombreArchivo}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                string filePath = _databaseService.DeleteDocument(SelectedDocumento.Id);
                if (File.Exists(filePath)) File.Delete(filePath);
                DocumentosMostrados.Remove(SelectedDocumento);
                SelectedDocumento = null;
            }
        }

        private void LoadAllDocuments()
        {
            DocumentosMostrados.Clear();
            foreach (var doc in _databaseService.GetAllDocuments()) DocumentosMostrados.Add(new DocumentoViewModel(doc));
        }

        private void CargarPdf()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Filter = "Archivos PDF|*.pdf", Multiselect = true };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string ruta in openFileDialog.FileNames)
                {
                    try
                    {
                        string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados", Path.GetFileName(ruta));
                        Directory.CreateDirectory(Path.GetDirectoryName(destino));
                        File.Copy(ruta, destino, true);
                        _databaseService.GuardarDocumento(Path.GetFileName(ruta), destino, _pdfService.ExtraerTextoDePdf(destino));
                    }
                    catch (Exception ex) { MessageBox.Show("Error al cargar: " + ex.Message); }
                }
                RefreshView();
            }
        }

        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;
            var resultados = _databaseService.BuscarTermino(TerminoBusqueda);
            bool encontrado = resultados.Count > 0;
            _nombresArchivosEncontrados = encontrado ? resultados.Select(d => d.NombreArchivo).ToList() : new List<string>();
            ResultadoEncontrado = encontrado;
            var ids = new HashSet<int>(resultados.Select(r => r.Id));
            foreach (var doc in DocumentosMostrados) doc.IsSearchResult = ids.Contains(doc.Id);
            if (SelectedDocumento != null) LoadPdfContent(SelectedDocumento.Id);
            _databaseService.RegistrarBusqueda(TerminoBusqueda, encontrado, SessionService.CurrentUserId);
        }

        private void UpdateConstanciaButtonStates()
        {
            bool canGen = !string.IsNullOrWhiteSpace(TerminoBusqueda);
            IsAprobatoriaButtonEnabled = canGen && !ResultadoEncontrado;
            IsDenegadaButtonEnabled = canGen && ResultadoEncontrado;
        }

        private void ClearSearchHighlights() { foreach (var doc in DocumentosMostrados) doc.IsSearchResult = false; }

        // --- MÉTODO CORREGIDO: ENVÍA LOS DATOS MANUALES AL PDF ---
        private void GenerarConstancia(bool parametro)
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;

            // Preparamos los datos capturados en los TextBox
            var infoManual = new DatosNotaria
            {
                NombreNotario = string.IsNullOrWhiteSpace(NombreNotarioInput) ? "NOMBRE NO ESPECIFICADO" : NombreNotarioInput,
                NumeroNotaria = string.IsNullOrWhiteSpace(NumeroNotariaInput) ? "0" : NumeroNotariaInput,
                DireccionCompleta = string.IsNullOrWhiteSpace(DireccionInput) ? "SIN DIRECCIÓN REGISTRADA" : DireccionInput,
                DatosContacto = string.IsNullOrWhiteSpace(ContactoInput) ? "" : ContactoInput
            };

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), $"Constancia_{TerminoBusqueda.Replace(" ", "_")}.pdf");

                // Llamamos al servicio pasando el nuevo objeto 'infoManual'
                _pdfService.GenerarConstancia(tempPath, TerminoBusqueda, !ResultadoEncontrado, _nombresArchivosEncontrados, infoManual);

                new PdfViewerWindow(tempPath).Show();
                RefreshView();
            }
            catch (Exception ex) { MessageBox.Show("Error PDF: " + ex.Message); }
        }

        private void LoadPdfContent(int docId)
        {
            try { ContenidoDocumento = CreateHighlightedFlowDocument(_databaseService.GetDocumentoContent(docId), TerminoBusqueda); }
            catch { ContenidoDocumento = new FlowDocument(); }
        }

        private void ShowHistory() => new HistoryWindow().Show();

        private void RefreshView()
        {
            TerminoBusqueda = string.Empty;
            SelectedDocumento = null;
            LoadAllDocuments();
            ContenidoDocumento = new FlowDocument();
        }

        // --- Métodos de resaltado (sin cambios estructurales) ---
        private FlowDocument CreateHighlightedFlowDocument(string text, string searchTerm)
        {
            // ... (Mismo código de resaltado que ya tienes)
            return new FlowDocument(); // Referencia simplificada
        }
        private void AddFormattedRun(Paragraph p, string t, Brush f, FontWeight w) => p.Inlines.Add(new Run(t) { Foreground = f, FontWeight = w });
        private void AddHighlightedTextToParagraph(Paragraph p, string t, string s) => CreateHighlightedRuns(t, s).ForEach(i => p.Inlines.Add(i));
        private List<Inline> CreateHighlightedRuns(string text, string searchTerm)
        {
            var inlines = new List<Inline>();
            // ... (Mismo código de resaltado que ya tienes)
            return inlines;
        }
    }

}