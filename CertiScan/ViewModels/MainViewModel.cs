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
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CertiScan.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;
        private readonly PdfSatService _pdfSatService;

        private string _fuenteHallazgoUif = string.Empty;
        private string _fuenteHallazgoSat = string.Empty;

        public string NombreUsuarioLogueado { get; }
        public ObservableCollection<DocumentoViewModel> DocumentosMostrados { get; set; }
        public ObservableCollection<DocumentoViewModel> DocumentosSatMostrados { get; set; }
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
            set { if (SetProperty(ref _terminoBusquedaSat, value)) { ResultadoEncontradoSat = false; UpdateSatButtonStates(false, false); } }
        }

        private bool _resultadoEncontradoSat = false;
        public bool ResultadoEncontradoSat { get => _resultadoEncontradoSat; set => SetProperty(ref _resultadoEncontradoSat, value); }

        private bool _isSatLimpioButtonEnabled;
        public bool IsSatLimpioButtonEnabled { get => _isSatLimpioButtonEnabled; set => SetProperty(ref _isSatLimpioButtonEnabled, value); }

        private bool _isSatHallazgoButtonEnabled;
        public bool IsSatHallazgoButtonEnabled { get => _isSatHallazgoButtonEnabled; set => SetProperty(ref _isSatHallazgoButtonEnabled, value); }

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
        public IRelayCommand<bool> GenerarReporteSatCommand { get; }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService();
            _pdfSatService = new PdfSatService();

            DocumentosMostrados = new ObservableCollection<DocumentoViewModel>();
            DocumentosSatMostrados = new ObservableCollection<DocumentoViewModel>();
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
            GenerarReporteSatCommand = new RelayCommand<bool>(GenerarReporteSat);

            NombreUsuarioLogueado = SessionService.CurrentUserName;
            LoadAllDocuments();
            LoadHistorial("UIF");
        }

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            string temp = Regex.Replace(texto.Trim(), @"\s+", " ").ToUpper();
            var normalizedString = temp.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private string LimpiarNombreParaRuta(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "Reporte";
            string seguro = NormalizarTexto(nombre);
            var sb = new StringBuilder();
            foreach (char c in seguro)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else sb.Append("_");
            }
            return sb.ToString();
        }

        public void LoadHistorial(string tipoModulo)
        {
            var historial = _databaseService.GetSearchHistory(SessionService.CurrentUserId, SessionService.CurrentUserName, tipoModulo);
            Application.Current.Dispatcher.Invoke(() => {
                HistorialBusquedas.Clear();
                foreach (var h in historial) HistorialBusquedas.Add(h);
            });
        }

        private void UpdateConstanciaButtonStates(bool realizada)
        {
            IsAprobatoriaButtonEnabled = realizada && !ResultadoEncontrado;
            IsDenegadaButtonEnabled = realizada && ResultadoEncontrado;
        }

        private void UpdateSatButtonStates(bool realizada, bool huboHallazgo)
        {
            ResultadoEncontradoSat = huboHallazgo;
            IsSatLimpioButtonEnabled = realizada && !huboHallazgo;
            IsSatHallazgoButtonEnabled = realizada && huboHallazgo;
            IsReporteSatEnabled = realizada;
        }

        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;
            string terminoLimpio = NormalizarTexto(TerminoBusqueda);
            var resultados = _databaseService.BuscarTermino(terminoLimpio, SessionService.CurrentUserId, "UIF");

            ResultadoEncontrado = resultados.Count > 0;
            _fuenteHallazgoUif = ResultadoEncontrado ? string.Join(", ", resultados.Select(d => d.NombreArchivo)) : string.Empty;

            UpdateConstanciaButtonStates(true);
            _databaseService.RegistrarBusqueda(TerminoBusqueda, ResultadoEncontrado, SessionService.CurrentUserId, "UIF");
            LoadHistorial("UIF");

            MessageBox.Show(ResultadoEncontrado ? "¡COINCIDENCIA ENCONTRADA EN UIF!" : "Sin coincidencias en UIF.");
        }

        private void BuscarSat()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusquedaSat)) return;

            // Se normaliza el término de búsqueda igual que en la extracción
            string terminoParaBD = NormalizarTexto(TerminoBusquedaSat);

            // Búsqueda ESTRICTA (Sin el fallback del primer apellido que causaba errores)
            var resultados = _databaseService.BuscarTermino(terminoParaBD, SessionService.CurrentUserId, "SAT");

            bool hallazgo = resultados.Count > 0;

            if (hallazgo)
            {
                // Priorizamos el archivo CSV si está entre los hallazgos
                var resultadoCsv = resultados.FirstOrDefault(d => d.NombreArchivo.ToLower().EndsWith(".csv"));
                _fuenteHallazgoSat = resultadoCsv != null ? resultadoCsv.NombreArchivo : resultados.First().NombreArchivo;
            }
            else
            {
                _fuenteHallazgoSat = string.Empty;
            }

            UpdateSatButtonStates(true, hallazgo);
            _databaseService.RegistrarBusqueda(TerminoBusquedaSat, hallazgo, SessionService.CurrentUserId, "SAT");
            LoadHistorial("SAT");

            if (hallazgo)
                MessageBox.Show($"🚨 ¡HALLAZGO EN SAT!\nEncontrado en: {_fuenteHallazgoSat}", "CertiScan", MessageBoxButton.OK, MessageBoxImage.Warning);
            else
                MessageBox.Show("✅ Sin coincidencias en el listado SAT.", "CertiScan", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GenerarConstancia(bool esAprobatoria)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;
                string nombreSeguro = LimpiarNombreParaRuta(TerminoBusqueda);
                string nombreFinal = $"Constancia_UIF_{nombreSeguro}_{DateTime.Now.Ticks}.pdf";
                string tempPath = Path.Combine(Path.GetTempPath(), nombreFinal);

                var info = _databaseService.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);
                var datos = new DatosNotaria { NombreNotario = info?.NombreNotario ?? "No Configurado", NumeroNotaria = info?.NumeroNotaria ?? "0", DireccionCompleta = info?.Direccion ?? "No Configurada", DatosContacto = $"Tel: {info?.Telefono}" };

                List<string> listaArchivos = esAprobatoria ? DocumentosMostrados.Select(d => d.NombreArchivo).ToList() : new List<string> { _fuenteHallazgoUif };

                _pdfService.GenerarConstancia(tempPath, TerminoBusqueda, esAprobatoria, listaArchivos, datos);
                new PdfViewerWindow(tempPath, TerminoBusqueda).ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show("Error al crear constancia UIF: " + ex.Message); }
        }

        private void GenerarReporteSat(bool esLimpio)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TerminoBusquedaSat)) return;

                string nombreSeguro = LimpiarNombreParaRuta(TerminoBusquedaSat);
                string nombreFinal = $"Reporte_SAT_{nombreSeguro}_{DateTime.Now.Ticks}.pdf";
                string tempPath = Path.Combine(Path.GetTempPath(), nombreFinal);

                var info = _databaseService.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);
                var datos = new DatosNotaria
                {
                    NombreNotario = info?.NombreNotario ?? "No Configurado",
                    NumeroNotaria = info?.NumeroNotaria ?? "0",
                    DireccionCompleta = info?.Direccion ?? "No Configurada"
                };

                List<string> listaParaPdf = esLimpio
                    ? DocumentosSatMostrados.Select(d => d.NombreArchivo).ToList()
                    : new List<string> { _fuenteHallazgoSat };

                _pdfSatService.GenerarReporteSat(tempPath, TerminoBusquedaSat, esLimpio, listaParaPdf, datos);
                new PdfViewerWindow(tempPath, TerminoBusquedaSat).ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private async Task CargarArchivoUniversalAsync(bool esParaSat)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "Archivos Compatibles|*.csv;*.pdf;*.txt" };
            if (ofd.ShowDialog() == true)
            {
                await Task.Run(() => ProcesarDocumentoIndividualAsync(ofd.FileName, esParaSat ? "SAT" : "UIF"));
                LoadAllDocuments();
            }
        }

        private async Task ProcesarDocumentoIndividualAsync(string ruta, string modulo)
        {
            try
            {
                string ext = Path.GetExtension(ruta).ToLower();
                string contenidoFinal = string.Empty;

                if (ext == ".csv")
                {
                    var nombresLimpios = new StringBuilder();
                    var lineas = File.ReadAllLines(ruta, Encoding.GetEncoding("Windows-1252"));

                    foreach (var linea in lineas.Skip(3))
                    {
                        var campos = linea.Split(',');
                        if (campos.Length > 2)
                        {
                            string nombreRaw = campos[2].Replace("\"", "");
                            nombresLimpios.AppendLine(NormalizarTexto(nombreRaw));
                        }
                    }
                    contenidoFinal = nombresLimpios.ToString();
                }
                else if (ext == ".pdf")
                {
                    contenidoFinal = modulo == "SAT" ? _pdfSatService.ExtraerTextoDePdf(ruta) : _pdfService.ExtraerTextoDePdf(ruta);
                    contenidoFinal = NormalizarTexto(contenidoFinal);
                }
                else
                {
                    contenidoFinal = NormalizarTexto(File.ReadAllText(ruta, Encoding.UTF8));
                }

                await _databaseService.GuardarDocumentoAsync(Path.GetFileName(ruta), ruta, contenidoFinal, SessionService.CurrentUserId, modulo);
            }
            catch (Exception ex) { Application.Current.Dispatcher.Invoke(() => MessageBox.Show(ex.Message)); }
        }

        public void LoadAllDocuments()
        {
            var u = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "UIF");
            var s = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "SAT");
            Application.Current.Dispatcher.Invoke(() => {
                DocumentosMostrados.Clear(); foreach (var d in u) DocumentosMostrados.Add(new DocumentoViewModel(d));
                DocumentosSatMostrados.Clear(); foreach (var d in s) DocumentosSatMostrados.Add(new DocumentoViewModel(d));
            });
        }

        private void RefreshView() { TerminoBusqueda = string.Empty; UpdateConstanciaButtonStates(false); LoadAllDocuments(); LoadHistorial("UIF"); }
        private void RefreshViewSat() { TerminoBusquedaSat = string.Empty; UpdateSatButtonStates(false, false); LoadAllDocuments(); LoadHistorial("SAT"); }
        private void DeletePdf() { if (SelectedDocumento != null) { _databaseService.DeleteDocument(SelectedDocumento.Id); RefreshView(); } }
        private void DeletePdfSat() { if (SelectedDocumentoSat != null) { _databaseService.DeleteDocument(SelectedDocumentoSat.Id); RefreshViewSat(); } }
    }
}