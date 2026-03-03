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
using System.Data; // <--- ESENCIAL PARA DATATABLE

namespace CertiScan.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;

        // --- PROPIEDADES COMPARTIDAS / NOTARÍA ---
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
        private DocumentoViewModel _selectedDocumento;
        public DocumentoViewModel SelectedDocumento
        {
            get => _selectedDocumento;
            set { if (SetProperty(ref _selectedDocumento, value)) { ContenidoDocumento = value != null ? _databaseService.GetDocumentoContent(value.Id) : string.Empty; DeletePdfCommand?.NotifyCanExecuteChanged(); } }
        }

        private string _contenidoDocumento;
        public string ContenidoDocumento { get => _contenidoDocumento; set => SetProperty(ref _contenidoDocumento, value); }
        public bool IsAprobatoriaButtonEnabled { get; private set; }
        public bool IsDenegadaButtonEnabled { get; private set; }

        // --- MÓDULO SAT (69-B) ---
        private string _terminoBusquedaSat = string.Empty;
        public string TerminoBusquedaSat { get => _terminoBusquedaSat; set => SetProperty(ref _terminoBusquedaSat, value); }

        private bool _isReporteSatEnabled = false;
        public bool IsReporteSatEnabled { get => _isReporteSatEnabled; set => SetProperty(ref _isReporteSatEnabled, value); }

        public ObservableCollection<DocumentoViewModel> DocumentosSatMostrados { get; set; }
        private DocumentoViewModel _selectedDocumentoSat;
        public DocumentoViewModel SelectedDocumentoSat
        {
            get => _selectedDocumentoSat;
            set { if (SetProperty(ref _selectedDocumentoSat, value)) ContenidoDocumentoSat = value != null ? _databaseService.GetDocumentoContent(value.Id) : string.Empty; }
        }

        private string _contenidoDocumentoSat;
        public string ContenidoDocumentoSat { get => _contenidoDocumentoSat; set => SetProperty(ref _contenidoDocumentoSat, value); }

        // --- COMANDOS ---
        public IRelayCommand CargarPdfCommand { get; }
        public IRelayCommand BuscarCommand { get; }
        public IRelayCommand<bool> GenerarConstanciaCommand { get; }
        public IRelayCommand DeletePdfCommand { get; }
        public IRelayCommand ShowHistoryCommand { get; }
        public IRelayCommand RefreshCommand { get; }

        public IRelayCommand CargarPdfSatCommand { get; }
        public IRelayCommand BuscarSatCommand { get; }
        public IRelayCommand<bool> GenerarReporteSatCommand { get; }
        public IRelayCommand DeletePdfSatCommand { get; }
        public IRelayCommand ShowHistorySatCommand { get; }
        public IRelayCommand RefreshSatCommand { get; }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService();
            DocumentosMostrados = new ObservableCollection<DocumentoViewModel>();
            DocumentosSatMostrados = new ObservableCollection<DocumentoViewModel>();

            CargarPdfCommand = new RelayCommand(() => CargarArchivoUniversal(esParaSat: false));
            BuscarCommand = new RelayCommand(Buscar);
            GenerarConstanciaCommand = new RelayCommand<bool>(GenerarConstancia);
            DeletePdfCommand = new RelayCommand(DeletePdf, () => SelectedDocumento != null);
            ShowHistoryCommand = new RelayCommand(() => new HistoryWindow().Show());
            RefreshCommand = new RelayCommand(RefreshView);

            CargarPdfSatCommand = new RelayCommand(() => CargarArchivoUniversal(esParaSat: true));
            BuscarSatCommand = new RelayCommand(BuscarSat);
            GenerarReporteSatCommand = new RelayCommand<bool>(GenerarReporteSat);
            DeletePdfSatCommand = new RelayCommand(DeletePdfSat, () => SelectedDocumentoSat != null);
            ShowHistorySatCommand = new RelayCommand(() => MessageBox.Show("Historial SAT en desarrollo"));
            RefreshSatCommand = new RelayCommand(RefreshSatView);

            NombreUsuarioLogueado = SessionService.CurrentUserName;
            LoadAllDocuments();
            CargarDatosNotariaDesdeBD();
        }

        private void CargarArchivoUniversal(bool esParaSat)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos Soportados (*.pdf;*.csv;*.txt)|*.pdf;*.csv;*.txt",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string ruta = openFileDialog.FileName;
                string extension = Path.GetExtension(ruta).ToLower();

                if (esParaSat && extension == ".csv")
                {
                    ProcesarCargaMasivaSat(ruta);
                }
                else
                {
                    ProcesarDocumentoIndividual(ruta, esParaSat ? "SAT" : "UIF");
                }
                LoadAllDocuments();
            }
        }

        private void ProcesarCargaMasivaSat(string ruta)
        {
            try
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("RFC");
                dt.Columns.Add("NombreContribuyente");
                dt.Columns.Add("Situacion");
                dt.Columns.Add("UsuarioId", typeof(int));

                var lineas = File.ReadLines(ruta, Encoding.UTF8).Skip(3);

                foreach (var linea in lineas)
                {
                    var campos = linea.Split(',');
                    if (campos.Length >= 4)
                    {
                        dt.Rows.Add(campos[1].Trim(), campos[2].Trim(), campos[3].Trim(), SessionService.CurrentUserId);
                    }
                }

                _databaseService.CargaMasivaListadoSat(dt);

                string nombreArchivo = Path.GetFileName(ruta);
                string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados", nombreArchivo);
                Directory.CreateDirectory(Path.GetDirectoryName(destino));
                if (!File.Exists(destino)) File.Copy(ruta, destino);

                _databaseService.GuardarDocumento(nombreArchivo, destino, "LISTADO MASIVO PROCESADO", SessionService.CurrentUserId, "SAT");
                MessageBox.Show("Listado 69-B importado con éxito.");
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
        }

        private void ProcesarDocumentoIndividual(string ruta, string modulo)
        {
            try
            {
                string extension = Path.GetExtension(ruta).ToLower();
                string contenido = extension == ".pdf" ? _pdfService.ExtraerTextoDePdf(ruta) : File.ReadAllText(ruta, Encoding.UTF8);

                string nombreArchivo = Path.GetFileName(ruta);
                string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados", nombreArchivo);

                Directory.CreateDirectory(Path.GetDirectoryName(destino));
                File.Copy(ruta, destino, true);

                _databaseService.GuardarDocumento(nombreArchivo, destino, contenido, SessionService.CurrentUserId, modulo);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
        }

        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;
            // Corregido: Se pasa el parámetro "UIF"
            var resultados = _databaseService.BuscarTermino(TerminoBusqueda, SessionService.CurrentUserId, "UIF");
            ResultadoEncontrado = resultados.Count > 0;
            UpdateConstanciaButtonStates(true);
            _databaseService.RegistrarBusqueda(TerminoBusqueda, ResultadoEncontrado, SessionService.CurrentUserId);
            MessageBox.Show(ResultadoEncontrado ? "¡COINCIDENCIA ENCONTRADA EN UIF!" : "No hay coincidencias.");
        }

        private void BuscarSat()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusquedaSat)) return;
            DataTable resultados = _databaseService.BuscarEnListadoSat(TerminoBusquedaSat);
            bool hallazgo = resultados.Rows.Count > 0;
            IsReporteSatEnabled = true;

            if (hallazgo)
            {
                string msg = $"¡HALLAZGO! contribuyente: {resultados.Rows[0]["NombreContribuyente"]}\nSituación: {resultados.Rows[0]["Situacion"]}";
                MessageBox.Show(msg, "Alerta SAT 69-B", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else { MessageBox.Show("Sin coincidencias en el listado 69-B.", "Resultado SAT"); }
        }

        private void LoadAllDocuments()
        {
            DocumentosMostrados.Clear();
            // Corregido: Se pasan los parámetros por módulo
            var docsUif = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "UIF");
            foreach (var doc in docsUif) DocumentosMostrados.Add(new DocumentoViewModel(doc));

            DocumentosSatMostrados.Clear();
            var docsSat = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "SAT");
            foreach (var doc in docsSat) DocumentosSatMostrados.Add(new DocumentoViewModel(doc));
        }

        private void UpdateConstanciaButtonStates(bool realizada)
        {
            IsAprobatoriaButtonEnabled = realizada && !ResultadoEncontrado;
            IsDenegadaButtonEnabled = realizada && ResultadoEncontrado;
            OnPropertyChanged(nameof(IsAprobatoriaButtonEnabled));
            OnPropertyChanged(nameof(IsDenegadaButtonEnabled));
        }

        private void CargarDatosNotariaDesdeBD()
        {
            if (SessionService.UsuarioLogueado == null) return;
            var info = _databaseService.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);
            if (info != null)
            {
                NombreNotarioInput = info.NombreNotario;
                NumeroNotariaInput = info.NumeroNotaria;
                DireccionInput = info.Direccion;
                ContactoInput = $"Tel: {info.Telefono} | Email: {info.Email}";
            }
        }

        private void DeletePdf() { if (SelectedDocumento != null) { _databaseService.DeleteDocument(SelectedDocumento.Id); RefreshView(); } }
        private void DeletePdfSat() { if (SelectedDocumentoSat != null) { _databaseService.DeleteDocument(SelectedDocumentoSat.Id); RefreshSatView(); } }
        private void RefreshView() { TerminoBusqueda = string.Empty; SelectedDocumento = null; LoadAllDocuments(); }
        private void RefreshSatView() { TerminoBusquedaSat = string.Empty; SelectedDocumentoSat = null; IsReporteSatEnabled = false; LoadAllDocuments(); }
        private void GenerarConstancia(bool p) { /* Lógica existente */ }
        private void GenerarReporteSat(bool esLimpio) { MessageBox.Show("Generando reporte..."); }
    }
}