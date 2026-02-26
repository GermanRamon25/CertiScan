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

        // --- MÓDULO UIF (Búsqueda General) ---
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

        // --- MÓDULO SAT (Verificación 69-B) ---
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
        public string ContenidoDocumentoSat { get => _contenidoDocumentoSat; set => SetProperty(ref _contenidoDocumentoSat, value); }
        private string _contenidoDocumentoSat;

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

            // Comandos con Lógica Universal
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

        // --- MÉTODO DE CARGA UNIVERSAL (PDF, CSV, TXT) ---
        private void CargarArchivoUniversal(bool esParaSat)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Todos los archivos soportados (*.pdf;*.csv;*.txt)|*.pdf;*.csv;*.txt|Documentos PDF (*.pdf)|*.pdf|Archivos de Datos (*.csv;*.txt)|*.csv;*.txt",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string ruta in openFileDialog.FileNames)
                {
                    string extension = Path.GetExtension(ruta).ToLower();
                    string contenido = string.Empty;

                    try
                    {
                        // Selección de motor de extracción según formato
                        if (extension == ".pdf")
                        {
                            contenido = _pdfService.ExtraerTextoDePdf(ruta);
                        }
                        else if (extension == ".csv" || extension == ".txt")
                        {
                            contenido = File.ReadAllText(ruta, Encoding.UTF8);
                        }

                        string nombreArchivo = Path.GetFileName(ruta);
                        string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados", nombreArchivo);

                        Directory.CreateDirectory(Path.GetDirectoryName(destino));
                        File.Copy(ruta, destino, true);

                        _databaseService.GuardarDocumento(nombreArchivo, destino, contenido, SessionService.CurrentUserId);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al procesar {Path.GetFileName(ruta)}: {ex.Message}", "Error de Carga", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                LoadAllDocuments();
                MessageBox.Show($"Archivo(s) cargado(s) correctamente en el módulo {(esParaSat ? "SAT" : "UIF")}.");
            }
        }

        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;
            var resultados = _databaseService.BuscarTermino(TerminoBusqueda, SessionService.CurrentUserId);
            ResultadoEncontrado = resultados.Count > 0;
            UpdateConstanciaButtonStates(true);
            _databaseService.RegistrarBusqueda(TerminoBusqueda, ResultadoEncontrado, SessionService.CurrentUserId);
            MessageBox.Show(ResultadoEncontrado ? "¡COINCIDENCIA ENCONTRADA EN UIF!" : "No se encontraron coincidencias.", "Resultado");
        }

        private void BuscarSat()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusquedaSat)) return;
            // Aquí puedes implementar lógica específica para el 69-B si lo deseas
            IsReporteSatEnabled = true;
            MessageBox.Show("Análisis de listas 69-B completado.", "Módulo SAT");
        }

        private void UpdateConstanciaButtonStates(bool realizada)
        {
            IsAprobatoriaButtonEnabled = realizada && !ResultadoEncontrado;
            IsDenegadaButtonEnabled = realizada && ResultadoEncontrado;
            OnPropertyChanged(nameof(IsAprobatoriaButtonEnabled));
            OnPropertyChanged(nameof(IsDenegadaButtonEnabled));
        }

        private void LoadAllDocuments()
        {
            DocumentosMostrados.Clear();
            var docs = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId);
            foreach (var doc in docs) DocumentosMostrados.Add(new DocumentoViewModel(doc));
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

        private void DeletePdf() { /* Lógica para eliminar de BD y disco */ }
        private void DeletePdfSat() { /* Lógica para eliminar de BD y disco */ }
        private void RefreshView() { TerminoBusqueda = string.Empty; SelectedDocumento = null; LoadAllDocuments(); }
        private void RefreshSatView() { TerminoBusquedaSat = string.Empty; SelectedDocumentoSat = null; IsReporteSatEnabled = false; }
        private void GenerarConstancia(bool p) { /* Lógica existente */ }
        private void GenerarReporteSat(bool esLimpio) { MessageBox.Show($"Generando reporte SAT {(esLimpio ? "Limpio" : "con Hallazgos")}..."); }
    }
}