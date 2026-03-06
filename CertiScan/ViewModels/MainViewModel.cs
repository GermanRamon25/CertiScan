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

        private string _fuenteHallazgoUif = string.Empty;

        public string NombreUsuarioLogueado { get; }
        public ObservableCollection<DocumentoViewModel> DocumentosMostrados { get; set; }
        public ObservableCollection<DocumentoViewModel> DocumentosSatMostrados { get; set; }

        // --- 1. ESTO ES LO QUE FALTABA: LA COLECCIÓN DEL HISTORIAL ---
        public ObservableCollection<BusquedaHistorial> HistorialBusquedas { get; set; }

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
            set { if (SetProperty(ref _selectedDocumento, value)) { ContenidoDocumento = value != null ? _databaseService.GetDocumentoContent(value.Id) : string.Empty; DeletePdfCommand?.NotifyCanExecuteChanged(); } }
        }

        private string _contenidoDocumento;
        public string ContenidoDocumento { get => _contenidoDocumento; set => SetProperty(ref _contenidoDocumento, value); }

        // --- MÓDULO SAT ---
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

        private DocumentoViewModel _selectedDocumentoSat;
        public DocumentoViewModel SelectedDocumentoSat
        {
            get => _selectedDocumentoSat;
            set { if (SetProperty(ref _selectedDocumentoSat, value)) { ContenidoDocumentoSat = value != null ? _databaseService.GetDocumentoContent(value.Id) : string.Empty; DeletePdfSatCommand?.NotifyCanExecuteChanged(); } }
        }

        private string _contenidoDocumentoSat;
        public string ContenidoDocumentoSat { get => _contenidoDocumentoSat; set => SetProperty(ref _contenidoDocumentoSat, value); }

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

            // --- 2. INICIALIZAR LA LISTA ---
            HistorialBusquedas = new ObservableCollection<BusquedaHistorial>();

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

            // CARGA INICIAL
            LoadAllDocuments();
            LoadHistorial();
        }

        // --- 3. MÉTODO PARA RECARGAR EL HISTORIAL DESDE LA BD ---
        public void LoadHistorial()
        {
            // Nota: En tu DatabaseService el método se llama GetSearchHistory
            var historial = _databaseService.GetSearchHistory(SessionService.CurrentUserId, SessionService.CurrentUserName);

            Application.Current.Dispatcher.Invoke(() => {
                HistorialBusquedas.Clear();
                foreach (var h in historial)
                {
                    HistorialBusquedas.Add(h);
                }
            });
        }

        private void UpdateConstanciaButtonStates(bool realizada)
        {
            IsAprobatoriaButtonEnabled = realizada && !ResultadoEncontrado;
            IsDenegadaButtonEnabled = realizada && ResultadoEncontrado;
        }

        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;
            var resultados = _databaseService.BuscarTermino(TerminoBusqueda, SessionService.CurrentUserId, "UIF");
            ResultadoEncontrado = resultados.Count > 0;

            _fuenteHallazgoUif = ResultadoEncontrado
                ? string.Join(", ", resultados.Select(d => d.NombreArchivo))
                : string.Empty;

            UpdateConstanciaButtonStates(true);

            // GUARDAR EN BD
            _databaseService.RegistrarBusqueda(TerminoBusqueda, ResultadoEncontrado, SessionService.CurrentUserId);

            // --- 4. REFRESCAR AL INSTANTE ---
            LoadHistorial();

            MessageBox.Show(ResultadoEncontrado ? "¡COINCIDENCIA ENCONTRADA EN UIF!" : "Sin coincidencias.");
        }

        private void BuscarSat()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusquedaSat)) return;
            var resultados = _databaseService.BuscarTermino(TerminoBusquedaSat, SessionService.CurrentUserId, "SAT");
            ResultadoEncontradoSat = resultados.Count > 0;
            IsReporteSatEnabled = true;

            _databaseService.RegistrarBusqueda(TerminoBusquedaSat, ResultadoEncontradoSat, SessionService.CurrentUserId);

            // REFRESCAR TAMBIÉN EN SAT
            LoadHistorial();

            MessageBox.Show(ResultadoEncontradoSat ? "¡ALERTA EN SAT!" : "Sin coincidencias en SAT.");
        }

        private void GenerarConstancia(bool esAprobatoria)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TerminoBusqueda)) { MessageBox.Show("Primero busque en UIF."); return; }

                string tempPath = Path.Combine(Path.GetTempPath(), $"CertiScan_Temp_{Guid.NewGuid()}.pdf");
                var info = _databaseService.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);

                var datos = new DatosNotaria
                {
                    NombreNotario = info?.NombreNotario ?? "Dato No Configurado",
                    NumeroNotaria = info?.NumeroNotaria ?? "0",
                    DireccionCompleta = info?.Direccion ?? "Sinaloa, México",
                    DatosContacto = $"Tel: {info?.Telefono} | Email: {info?.Email}"
                };

                List<string> listaArchivos = esAprobatoria ? new List<string>() : new List<string> { _fuenteHallazgoUif };

                _pdfService.GenerarConstancia(tempPath, TerminoBusqueda, esAprobatoria, listaArchivos, datos);

                // --- 5. PASAR NOMBRE PARA EL GUARDADO AUTOMÁTICO ---
                var viewer = new PdfViewerWindow(tempPath, TerminoBusqueda);
                viewer.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void RefreshView() { LoadAllDocuments(); LoadHistorial(); }
        private void RefreshViewSat() { TerminoBusquedaSat = string.Empty; IsReporteSatEnabled = false; LoadAllDocuments(); LoadHistorial(); }

        private async Task CargarArchivoUniversalAsync(bool esParaSat) { var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "PDF|*.pdf|TXT|*.txt" }; if (ofd.ShowDialog() == true) { await Task.Run(() => ProcesarDocumentoIndividualAsync(ofd.FileName, esParaSat ? "SAT" : "UIF")); LoadAllDocuments(); } }
        private async Task ProcesarDocumentoIndividualAsync(string r, string m) { try { string ext = Path.GetExtension(r).ToLower(); string c = ext == ".pdf" ? _pdfService.ExtraerTextoDePdf(r) : File.ReadAllText(r, Encoding.UTF8); await _databaseService.GuardarDocumentoAsync(Path.GetFileName(r), r, c, SessionService.CurrentUserId, m); } catch (Exception ex) { Application.Current.Dispatcher.Invoke(() => MessageBox.Show(ex.Message)); } }
        public void LoadAllDocuments() { var u = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "UIF"); var s = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "SAT"); Application.Current.Dispatcher.Invoke(() => { DocumentosMostrados.Clear(); foreach (var d in u) DocumentosMostrados.Add(new DocumentoViewModel(d)); DocumentosSatMostrados.Clear(); foreach (var d in s) DocumentosSatMostrados.Add(new DocumentoViewModel(d)); }); }
        private void DeletePdf() { if (SelectedDocumento != null) { _databaseService.DeleteDocument(SelectedDocumento.Id); RefreshView(); } }
        private void DeletePdfSat() { if (SelectedDocumentoSat != null) { _databaseService.DeleteDocument(SelectedDocumentoSat.Id); RefreshViewSat(); } }
    }
}