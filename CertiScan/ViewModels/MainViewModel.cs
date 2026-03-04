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

namespace CertiScan.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;

        // --- PROPIEDADES NOTARÍA ---
        public string NombreUsuarioLogueado { get; }
        public string NombreNotarioInput { get; set; }
        public string NumeroNotariaInput { get; set; }
        public string DireccionInput { get; set; }
        public string ContactoInput { get; set; }

        // --- MÓDULO UIF ---
        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda
        {
            get => _terminoBusqueda;
            set { if (SetProperty(ref _terminoBusqueda, value)) { ResultadoEncontrado = false; UpdateConstanciaButtonStates(false); } }
        }

        private bool _resultadoEncontrado = false;
        public bool ResultadoEncontrado { get => _resultadoEncontrado; set => SetProperty(ref _resultadoEncontrado, value); }

        public ObservableCollection<DocumentoViewModel> DocumentosMostrados { get; set; }
        public ObservableCollection<DocumentoViewModel> DocumentosSatMostrados { get; set; }

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

        // Propiedades de estado para los botones de la Constancia
        private bool _isAprobatoriaButtonEnabled;
        public bool IsAprobatoriaButtonEnabled
        {
            get => _isAprobatoriaButtonEnabled;
            set => SetProperty(ref _isAprobatoriaButtonEnabled, value);
        }

        private bool _isDenegadaButtonEnabled;
        public bool IsDenegadaButtonEnabled
        {
            get => _isDenegadaButtonEnabled;
            set => SetProperty(ref _isDenegadaButtonEnabled, value);
        }

        // --- MÓDULO SAT ---
        public string TerminoBusquedaSat { get; set; }
        public bool IsReporteSatEnabled { get; set; }
        public string ContenidoDocumentoSat { get; set; }

        // --- COMANDOS ---
        public IAsyncRelayCommand CargarPdfCommand { get; }
        public IAsyncRelayCommand CargarPdfSatCommand { get; }
        public IRelayCommand BuscarCommand { get; }
        public IRelayCommand BuscarSatCommand { get; }
        public IRelayCommand DeletePdfCommand { get; }
        public IRelayCommand ShowHistoryCommand { get; }
        public IRelayCommand RefreshCommand { get; }
        public IRelayCommand<bool> GenerarConstanciaCommand { get; } // Comando para los botones de constancia

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
            ShowHistoryCommand = new RelayCommand(() => new HistoryWindow().Show());
            RefreshCommand = new RelayCommand(RefreshView);

            // Reintegración del comando para generar la constancia
            GenerarConstanciaCommand = new RelayCommand<bool>(GenerarConstancia);

            NombreUsuarioLogueado = SessionService.CurrentUserName;
            LoadAllDocuments();
            CargarDatosNotariaDesdeBD();
        }

        // --- LÓGICA DE CONSTANCIAS ---
        private void GenerarConstancia(bool esAprobatoria)
        {
            // Aquí puedes llamar a tu lógica de PdfService para crear el archivo
            MessageBox.Show(esAprobatoria
                ? "Generando Constancia Aprobatoria (Sin Hallazgos)..."
                : "Generando Constancia de Hallazgo (Denegada)...");
        }

        private void UpdateConstanciaButtonStates(bool realizada)
        {
            IsAprobatoriaButtonEnabled = realizada && !ResultadoEncontrado;
            IsDenegadaButtonEnabled = realizada && ResultadoEncontrado;
        }

        // --- BÚSQUEDA ---
        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;
            var resultados = _databaseService.BuscarTermino(TerminoBusqueda, SessionService.CurrentUserId, "UIF");
            ResultadoEncontrado = resultados.Count > 0;

            // Activamos los botones después de la búsqueda
            UpdateConstanciaButtonStates(true);

            _databaseService.RegistrarBusqueda(TerminoBusqueda, ResultadoEncontrado, SessionService.CurrentUserId);
            MessageBox.Show(ResultadoEncontrado ? "¡COINCIDENCIA ENCONTRADA EN UIF!" : "No hay coincidencias.");
        }

        // --- CARGA ASÍNCRONA ---
        private async Task CargarArchivoUniversalAsync(bool esParaSat)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Filter = "Archivos Soportados (*.pdf;*.csv;*.txt)|*.pdf;*.csv;*.txt" };

            if (openFileDialog.ShowDialog() == true)
            {
                string ruta = openFileDialog.FileName;
                await Task.Run(async () =>
                {
                    try
                    {
                        if (esParaSat && Path.GetExtension(ruta).ToLower() == ".csv")
                            await ProcesarCargaMasivaSatAsync(ruta);
                        else
                            await ProcesarDocumentoIndividualAsync(ruta, esParaSat ? "SAT" : "UIF");
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Error al procesar: {ex.Message}"));
                    }
                });
                LoadAllDocuments();
            }
        }

        private async Task ProcesarCargaMasivaSatAsync(string ruta)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("RFC"); dt.Columns.Add("NombreContribuyente"); dt.Columns.Add("Situacion"); dt.Columns.Add("UsuarioId", typeof(int));
            foreach (var linea in File.ReadLines(ruta, Encoding.UTF8).Skip(3))
            {
                var campos = linea.Split(',');
                if (campos.Length >= 4) dt.Rows.Add(campos[1].Trim(), campos[2].Trim(), campos[3].Trim(), SessionService.CurrentUserId);
            }
            await _databaseService.CargaMasivaListadoSatAsync(dt);
            await _databaseService.GuardarDocumentoAsync(Path.GetFileName(ruta), ruta, "LISTADO MASIVO", SessionService.CurrentUserId, "SAT");
        }

        private async Task ProcesarDocumentoIndividualAsync(string ruta, string modulo)
        {
            string extension = Path.GetExtension(ruta).ToLower();
            string contenido = extension == ".pdf" ? _pdfService.ExtraerTextoDePdf(ruta) : File.ReadAllText(ruta, Encoding.UTF8);
            await _databaseService.GuardarDocumentoAsync(Path.GetFileName(ruta), ruta, contenido, SessionService.CurrentUserId, modulo);
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
        private void RefreshView() { LoadAllDocuments(); }
        private void CargarDatosNotariaDesdeBD() { /* Lógica de notaría */ }
        private void BuscarSat() { /* Lógica SAT */ }
    }
}