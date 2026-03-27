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
            string temp = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in temp)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            string limpio = sb.ToString().Normalize(NormalizationForm.FormC).ToUpper();
            limpio = Regex.Replace(limpio, @"[^A-Z0-9\s]", " ");
            return Regex.Replace(limpio, @"\s+", " ").Trim();
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

            // VALIDACIÓN: Mínimo 2 palabras para Nombre Completo
            string[] palabras = TerminoBusqueda.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (palabras.Length < 2)
            {
                MessageBox.Show("Por favor, ingrese el nombre completo (mínimo un nombre y un apellido) para asegurar la validez de la búsqueda.", "Aviso de Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nombreBuscado = NormalizarTexto(TerminoBusqueda);
            var documentosBd = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "UIF");
            bool hallazgoUif = false;
            List<string> archivosConCoincidencia = new List<string>();

            string patron = $@"\b{Regex.Escape(nombreBuscado)}\b";

            foreach (var doc in documentosBd)
            {
                string contenidoOriginal = doc.ContenidoTexto ?? string.Empty;
                if (string.IsNullOrEmpty(contenidoOriginal)) continue;
                string contenidoNormalizado = NormalizarTexto(contenidoOriginal);

                if (Regex.IsMatch(contenidoNormalizado, patron))
                {
                    hallazgoUif = true;
                    if (!archivosConCoincidencia.Contains(doc.NombreArchivo))
                        archivosConCoincidencia.Add(doc.NombreArchivo);
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                ResultadoEncontrado = hallazgoUif;
                _fuenteHallazgoUif = ResultadoEncontrado ? string.Join(", ", archivosConCoincidencia) : string.Empty;
                UpdateConstanciaButtonStates(true);

                // TARJETA DE AVISO RESTAURADA
                if (ResultadoEncontrado)
                    MessageBox.Show($"¡COINCIDENCIA EXACTA ENCONTRADA!\nSe localizó el nombre completo: {nombreBuscado}\nEn: {_fuenteHallazgoUif}", "CertiScan - Módulo UIF", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show($"No se encontraron coincidencias exactas para: {nombreBuscado}", "CertiScan - Módulo UIF", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            _databaseService.RegistrarBusqueda(TerminoBusqueda, hallazgoUif, SessionService.CurrentUserId, "UIF");
            LoadHistorial("UIF");
        }

        private void BuscarSat()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusquedaSat)) return;

            // VALIDACIÓN SAT: Mínimo 2 palabras
            string[] palabras = TerminoBusquedaSat.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (palabras.Length < 2)
            {
                MessageBox.Show("Ingrese nombre y apellido para realizar la búsqueda en los listados del SAT.", "Aviso de Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string terminoParaBD = NormalizarTexto(TerminoBusquedaSat);
            var resultados = _databaseService.BuscarTermino(terminoParaBD, SessionService.CurrentUserId, "SAT");
            bool hallazgo = resultados.Count > 0;
            if (hallazgo)
            {
                var resultadoCsv = resultados.FirstOrDefault(d => d.NombreArchivo.ToLower().EndsWith(".csv"));
                _fuenteHallazgoSat = resultadoCsv != null ? resultadoCsv.NombreArchivo : resultados.First().NombreArchivo;
            }
            else _fuenteHallazgoSat = string.Empty;

            Application.Current.Dispatcher.Invoke(() => {
                UpdateSatButtonStates(true, hallazgo);

                // TARJETA DE AVISO RESTAURADA SAT
                if (hallazgo)
                    MessageBox.Show($"🚨 ¡HALLAZGO EN SAT!\nSe encontró coincidencia en: {_fuenteHallazgoSat}", "CertiScan - Módulo SAT", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show("✅ Sin coincidencias en los listados del SAT.", "CertiScan - Módulo SAT", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            _databaseService.RegistrarBusqueda(TerminoBusquedaSat, hallazgo, SessionService.CurrentUserId, "SAT");
            LoadHistorial("SAT");
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
            catch (Exception ex) { MessageBox.Show("Error al generar PDF: " + ex.Message); }
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
                var datos = new DatosNotaria { NombreNotario = info?.NombreNotario ?? "No Configurado", NumeroNotaria = info?.NumeroNotaria ?? "0", DireccionCompleta = info?.Direccion ?? "No Configurada" };
                List<string> listaParaPdf = esLimpio ? DocumentosSatMostrados.Select(d => d.NombreArchivo).ToList() : new List<string> { _fuenteHallazgoSat };
                _pdfSatService.GenerarReporteSat(tempPath, TerminoBusquedaSat, esLimpio, listaParaPdf, datos);
                new PdfViewerWindow(tempPath, TerminoBusquedaSat).ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show("Error al generar PDF SAT: " + ex.Message); }
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
                    string textoCompleto = File.ReadAllText(ruta, Encoding.GetEncoding("Windows-1252"));
                    var lineasCsv = Regex.Split(textoCompleto, @"\r?\n(?=(?:[^\""]*\""[^\""]*\"")*[^\""]*$)");
                    foreach (var linea in lineasCsv)
                    {
                        var campos = Regex.Split(linea, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                        if (campos.Length > 2)
                        {
                            string nombreRaw = campos[2].Trim();
                            if (!string.IsNullOrEmpty(nombreRaw) && !nombreRaw.Contains("Nombre del Contribuyente"))
                                nombresLimpios.AppendLine(nombreRaw);
                        }
                    }
                    contenidoFinal = nombresLimpios.ToString();
                }
                else if (ext == ".pdf")
                {
                    contenidoFinal = modulo == "SAT" ? _pdfSatService.ExtraerTextoDePdf(ruta) : _pdfService.ExtraerTextoDePdf(ruta);
                }
                else contenidoFinal = File.ReadAllText(ruta, Encoding.UTF8);

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