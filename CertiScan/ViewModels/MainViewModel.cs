using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CertiScan.Services;
using System;
using System.IO;
using CertiScan.Models;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;

namespace CertiScan.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;

        public string NombreUsuarioLogueado { get; }
        public ObservableCollection<DocumentoViewModel> DocumentosMostrados { get; set; }
        public ObservableCollection<DocumentoViewModel> DocumentosSatMostrados { get; set; }

        // --- MÓDULO UIF ---
        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda
        {
            get => _terminoBusqueda;
            set { if (SetProperty(ref _terminoBusqueda, value)) { ResultadoEncontrado = false; UpdateConstanciaButtonStates(false); } }
        }

        private bool _resultadoEncontrado = false;
        public bool ResultadoEncontrado { get => _resultadoEncontrado; set => SetProperty(ref _resultadoEncontrado, value); }

        private bool _isAprobatoriaButtonEnabled;
        public bool IsAprobatoriaButtonEnabled { get => _isAprobatoriaButtonEnabled; set => SetProperty(ref _isAprobatoriaButtonEnabled, value); }

        private bool _isDenegadaButtonEnabled;
        public bool IsDenegadaButtonEnabled { get => _isDenegadaButtonEnabled; set => SetProperty(ref _isDenegadaButtonEnabled, value); }

        private DocumentoViewModel _selectedDocumento;
        public DocumentoViewModel SelectedDocumento
        {
            get => _selectedDocumento;
            set
            {
                if (SetProperty(ref _selectedDocumento, value))
                {
                    ContenidoDocumento = value != null ? _databaseService.GetDocumentoContent(value.Id) : string.Empty;
                    DeletePdfCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        private string _contenidoDocumento;
        public string ContenidoDocumento { get => _contenidoDocumento; set => SetProperty(ref _contenidoDocumento, value); }

        // --- MÓDULO SAT (69-B) ---
        private string _terminoBusquedaSat = string.Empty;
        public string TerminoBusquedaSat
        {
            get => _terminoBusquedaSat;
            set { if (SetProperty(ref _terminoBusquedaSat, value)) { ResultadoEncontradoSat = false; IsReporteSatEnabled = false; } }
        }

        private bool _resultadoEncontradoSat = false;
        public bool ResultadoEncontradoSat { get => _resultadoEncontradoSat; set => SetProperty(ref _resultadoEncontradoSat, value); }

        private bool _isReporteSatEnabled;
        public bool IsReporteSatEnabled { get => _isReporteSatEnabled; set => SetProperty(ref _isReporteSatEnabled, value); }

        private string _contenidoDocumentoSat;
        public string ContenidoDocumentoSat { get => _contenidoDocumentoSat; set => SetProperty(ref _contenidoDocumentoSat, value); }

        private DocumentoViewModel _selectedDocumentoSat;
        public DocumentoViewModel SelectedDocumentoSat
        {
            get => _selectedDocumentoSat;
            set
            {
                if (SetProperty(ref _selectedDocumentoSat, value))
                {
                    ContenidoDocumentoSat = value != null ? _databaseService.GetDocumentoContent(value.Id) : string.Empty;
                    DeletePdfSatCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        // --- COMANDOS ---
        public IAsyncRelayCommand CargarPdfCommand { get; }
        public IAsyncRelayCommand CargarPdfSatCommand { get; }
        public IRelayCommand BuscarCommand { get; }
        public IRelayCommand BuscarSatCommand { get; }
        public IRelayCommand DeletePdfCommand { get; }
        public IRelayCommand DeletePdfSatCommand { get; }
        public IRelayCommand RefreshCommand { get; }
        public IRelayCommand RefreshSatCommand { get; }
        public IRelayCommand<bool> GenerarConstanciaCommand { get; }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService();
            DocumentosMostrados = new ObservableCollection<DocumentoViewModel>();
            DocumentosSatMostrados = new ObservableCollection<DocumentoViewModel>();

            CargarPdfCommand = new AsyncRelayCommand(() => CargarArchivoUniversalAsync(false));
            CargarPdfSatCommand = new AsyncRelayCommand(() => CargarArchivoUniversalAsync(true));

            BuscarCommand = new RelayCommand(Buscar);
            BuscarSatCommand = new RelayCommand(BuscarSat);

            DeletePdfCommand = new RelayCommand(DeletePdf, () => SelectedDocumento != null);
            DeletePdfSatCommand = new RelayCommand(DeletePdfSat, () => SelectedDocumentoSat != null);

            RefreshCommand = new RelayCommand(RefreshView);
            RefreshSatCommand = new RelayCommand(RefreshViewSat);

            GenerarConstanciaCommand = new RelayCommand<bool>(GenerarConstancia);

            NombreUsuarioLogueado = SessionService.CurrentUserName;
            LoadAllDocuments();
        }

        // MÉTODO CLAVE: Normaliza el texto para que la búsqueda ignore tildes y mayúsculas
        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var normalizedString = texto.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark) stringBuilder.Append(c);
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLower().Trim();
        }

        private void UpdateConstanciaButtonStates(bool realizada)
        {
            IsAprobatoriaButtonEnabled = realizada && !ResultadoEncontrado;
            IsDenegadaButtonEnabled = realizada && ResultadoEncontrado;
        }

        // Búsqueda UIF (Se mantiene igual, ya que funciona bien)
        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;
            var resultados = _databaseService.BuscarTermino(TerminoBusqueda, SessionService.CurrentUserId, "UIF");
            ResultadoEncontrado = resultados.Count > 0;
            UpdateConstanciaButtonStates(true);
            _databaseService.RegistrarBusqueda(TerminoBusqueda, ResultadoEncontrado, SessionService.CurrentUserId);
            MessageBox.Show(ResultadoEncontrado ? "¡COINCIDENCIA ENCONTRADA EN UIF!" : "Sin coincidencias en UIF.");
        }

        // CORRECCIÓN: Búsqueda SAT (Ahora funciona igual que la de UIF, buscando en los archivos cargados)
        private void BuscarSat()
        {
            string nombreBuscado = NormalizarTexto(TerminoBusquedaSat);
            if (string.IsNullOrWhiteSpace(nombreBuscado)) return;

            // Buscamos en los archivos que se han cargado en el módulo SAT
            var resultados = _databaseService.BuscarTermino(TerminoBusquedaSat, SessionService.CurrentUserId, "SAT");

            ResultadoEncontradoSat = resultados.Count > 0;
            IsReporteSatEnabled = true;

            // Registramos en historial
            _databaseService.RegistrarBusqueda(TerminoBusquedaSat, ResultadoEncontradoSat, SessionService.CurrentUserId);

            if (ResultadoEncontradoSat)
            {
                MessageBox.Show("🚨 ¡ATENCIÓN! El nombre se encuentra en los documentos del Listado 69-B.",
                                "CertiScan - SAT", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("✅ El nombre NO se encuentra en los documentos del Listado 69-B.",
                                "CertiScan - SAT", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GenerarConstancia(bool esAprobatoria)
        {
            MessageBox.Show(esAprobatoria ? "Generando Constancia Aprobatoria..." : "Generando Constancia de Hallazgo...");
        }

        private async Task CargarArchivoUniversalAsync(bool esParaSat)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Filter = "Archivos PDF (*.pdf)|*.pdf|Archivos de Texto (*.txt)|*.txt" };
            if (openFileDialog.ShowDialog() == true)
            {
                string ruta = openFileDialog.FileName;
                // Ahora ambos cargan archivos individuales (PDF/TXT)
                await Task.Run(() => ProcesarDocumentoIndividualAsync(ruta, esParaSat ? "SAT" : "UIF"));
                LoadAllDocuments();
            }
        }

        private async Task ProcesarDocumentoIndividualAsync(string ruta, string modulo)
        {
            try
            {
                string extension = Path.GetExtension(ruta).ToLower();
                string contenido = extension == ".pdf" ? _pdfService.ExtraerTextoDePdf(ruta) : File.ReadAllText(ruta, Encoding.UTF8);

                // No truncamos el contenido para la base de datos para que la búsqueda sea completa, 
                // pero si es muy pesado para la UI, puedes dejar el límite de 200,000 caracteres solo para la vista.
                await _databaseService.GuardarDocumentoAsync(Path.GetFileName(ruta), ruta, contenido, SessionService.CurrentUserId, modulo);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Error al cargar archivo: {ex.Message}"));
            }
        }

        public void LoadAllDocuments()
        {
            var docsUif = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "UIF");
            var docsSat = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "SAT");

            Application.Current.Dispatcher.Invoke(() => {
                DocumentosMostrados.Clear();
                foreach (var d in docsUif) DocumentosMostrados.Add(new DocumentoViewModel(d));

                DocumentosSatMostrados.Clear();
                foreach (var d in docsSat) DocumentosSatMostrados.Add(new DocumentoViewModel(d));
            });
        }

        private void DeletePdf() { if (SelectedDocumento != null) { _databaseService.DeleteDocument(SelectedDocumento.Id); RefreshView(); } }

        private void DeletePdfSat()
        {
            if (SelectedDocumentoSat != null)
            {
                _databaseService.DeleteDocument(SelectedDocumentoSat.Id);
                RefreshViewSat();
            }
        }

        private void RefreshView() { LoadAllDocuments(); }

        private void RefreshViewSat()
        {
            TerminoBusquedaSat = string.Empty;
            IsReporteSatEnabled = false;
            LoadAllDocuments();
        }
    }
}