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


        // --- MÓDULO SAT ---

        // CORRECCIÓN: Convertimos estas propiedades en Observables para que la UI reaccione a los cambios
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
        public string ContenidoDocumentoSat
        {
            get => _contenidoDocumentoSat;
            set => SetProperty(ref _contenidoDocumentoSat, value);
        }

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
        public IRelayCommand BuscarSatCommand { get; } // Nuevo: Comando para Buscar en SAT
        public IRelayCommand DeletePdfCommand { get; }
        public IRelayCommand DeletePdfSatCommand { get; }
        public IRelayCommand RefreshCommand { get; }
        public IRelayCommand RefreshSatCommand { get; } // Nuevo: Comando para Refrescar en SAT
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
            BuscarSatCommand = new RelayCommand(BuscarSat); // Inicialización 

            DeletePdfCommand = new RelayCommand(DeletePdf, () => SelectedDocumento != null);
            DeletePdfSatCommand = new RelayCommand(DeletePdfSat, () => SelectedDocumentoSat != null);

            RefreshCommand = new RelayCommand(RefreshView);
            RefreshSatCommand = new RelayCommand(RefreshViewSat); // Inicialización

            GenerarConstanciaCommand = new RelayCommand<bool>(GenerarConstancia);

            NombreUsuarioLogueado = SessionService.CurrentUserName;
            LoadAllDocuments();
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
            UpdateConstanciaButtonStates(true);
            _databaseService.RegistrarBusqueda(TerminoBusqueda, ResultadoEncontrado, SessionService.CurrentUserId);
            MessageBox.Show(ResultadoEncontrado ? "¡COINCIDENCIA ENCONTRADA!" : "Sin coincidencias.");
        }

        // --- MÉTODO AÑADIDO Y CORREGIDO: BUSCAR EN SAT ---
        private void BuscarSat()
        {
            // Limpiamos los espacios en blanco que el usuario pudo teclear por accidente
            string terminoLimpio = TerminoBusquedaSat?.Trim();

            if (string.IsNullOrWhiteSpace(terminoLimpio)) return;

            // Busca en la base de datos usando tu método existente
            DataTable resultados = _databaseService.BuscarEnListadoSat(terminoLimpio);
            ResultadoEncontradoSat = resultados.Rows.Count > 0;

            // Habilitamos los botones de reportes tras realizar la búsqueda
            IsReporteSatEnabled = true;

            // Registramos la búsqueda en el historial
            _databaseService.RegistrarBusqueda(terminoLimpio, ResultadoEncontradoSat, SessionService.CurrentUserId);

            // Mostramos los resultados al usuario
            if (ResultadoEncontradoSat)
            {
                string rfc = resultados.Rows[0]["RFC"].ToString();
                string nombre = resultados.Rows[0]["NombreContribuyente"].ToString();
                string situacion = resultados.Rows[0]["Situacion"].ToString();

                MessageBox.Show($"¡ALERTA! Coincidencia en listas negras (69-B):\n\nRFC: {rfc}\nNombre: {nombre}\nSituación: {situacion}",
                                "CertiScan - SAT", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("Sin coincidencias. El contribuyente no se encuentra en las listas del 69-B (Limpio).",
                                "CertiScan - SAT", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GenerarConstancia(bool esAprobatoria)
        {
            MessageBox.Show(esAprobatoria ? "Generando Constancia Aprobatoria..." : "Generando Constancia de Hallazgo...");
        }

        private async Task CargarArchivoUniversalAsync(bool esParaSat)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Filter = "Archivos (*.pdf;*.csv;*.txt)|*.pdf;*.csv;*.txt" };
            if (openFileDialog.ShowDialog() == true)
            {
                string ruta = openFileDialog.FileName;
                if (esParaSat && Path.GetExtension(ruta).ToLower() == ".csv")
                    await Task.Run(() => ProcesarCargaMasivaSatAsync(ruta));
                else
                    await Task.Run(() => ProcesarDocumentoIndividualAsync(ruta, esParaSat ? "SAT" : "UIF"));

                LoadAllDocuments();
            }
        }

        private async Task ProcesarCargaMasivaSatAsync(string ruta)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("No");
            dt.Columns.Add("RFC");
            dt.Columns.Add("NombreContribuyente");
            dt.Columns.Add("Situacion");
            dt.Columns.Add("UsuarioId", typeof(int));

            try
            {
                using (var parser = new TextFieldParser(ruta, Encoding.UTF8))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;
                    parser.TrimWhiteSpace = true;

                    while (!parser.EndOfData)
                    {
                        string[] fila = parser.ReadFields();
                        if (fila != null && fila.Length > 0 && fila[0].Trim().Equals("No", StringComparison.OrdinalIgnoreCase))
                            break;
                    }

                    while (!parser.EndOfData)
                    {
                        try
                        {
                            string[] campos = parser.ReadFields();
                            if (campos != null && campos.Length >= 4 && int.TryParse(campos[0], out _))
                            {
                                dt.Rows.Add(campos[0].Trim(), campos[1].Trim(), campos[2].Trim(), campos[3].Trim(), SessionService.CurrentUserId);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            if (dt.Rows.Count > 0)
            {
                await _databaseService.CargaMasivaListadoSatAsync(dt);

                string contenidoVista = "";
                try
                {
                    string textoCompleto = File.ReadAllText(ruta, Encoding.UTF8);
                    if (textoCompleto.Length > 200000)
                    {
                        contenidoVista = textoCompleto.Substring(0, 200000) + "\n\n... [VISTA PREVIA RECORTADA] ...";
                    }
                    else
                    {
                        contenidoVista = textoCompleto;
                    }
                }
                catch { contenidoVista = "No se pudo extraer el texto."; }

                await _databaseService.GuardarDocumentoAsync(Path.GetFileName(ruta), ruta, contenidoVista, SessionService.CurrentUserId, "SAT");

                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show($"Carga finalizada. Se importaron {dt.Rows.Count} registros.", "CertiScan"));
            }
        }

        private async Task ProcesarDocumentoIndividualAsync(string ruta, string modulo)
        {
            try
            {
                string extension = Path.GetExtension(ruta).ToLower();
                string contenido = extension == ".pdf" ? _pdfService.ExtraerTextoDePdf(ruta) : File.ReadAllText(ruta, Encoding.UTF8);

                if (contenido.Length > 200000)
                {
                    contenido = contenido.Substring(0, 200000) + "\n\n... [VISTA PREVIA RECORTADA] ...";
                }

                await _databaseService.GuardarDocumentoAsync(Path.GetFileName(ruta), ruta, contenido, SessionService.CurrentUserId, modulo);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Error: {ex.Message}"));
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
                RefreshView();
            }
        }

        private void RefreshView() { LoadAllDocuments(); }

        // --- MÉTODO AÑADIDO: REFRESCAR SAT ---
        private void RefreshViewSat()
        {
            TerminoBusquedaSat = string.Empty; // Limpia la caja de texto
            IsReporteSatEnabled = false;       // Deshabilita los botones de reporte para la próxima búsqueda
            LoadAllDocuments();                // Recarga la lista
        }
    }
}