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
using System.Threading.Tasks; // <--- IMPRESCINDIBLE

namespace CertiScan.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;

        // --- PROPIEDADES ---
        public string NombreUsuarioLogueado { get; }
        public string NombreNotarioInput { get; set; }
        public string NumeroNotariaInput { get; set; }
        public string DireccionInput { get; set; }
        public string ContactoInput { get; set; }

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
            set { if (SetProperty(ref _selectedDocumento, value)) { ContenidoDocumento = value != null ? _databaseService.GetDocumentoContent(value.Id) : string.Empty; DeletePdfCommand?.NotifyCanExecuteChanged(); } }
        }

        private string _contenidoDocumento;
        public string ContenidoDocumento { get => _contenidoDocumento; set => SetProperty(ref _contenidoDocumento, value); }

        public bool IsAprobatoriaButtonEnabled { get; private set; }
        public bool IsDenegadaButtonEnabled { get; private set; }

        public string TerminoBusquedaSat { get; set; }
        public bool IsReporteSatEnabled { get; set; }
        public string ContenidoDocumentoSat { get; set; }

        // --- COMANDOS ---
        public IAsyncRelayCommand CargarPdfCommand { get; }
        public IAsyncRelayCommand CargarPdfSatCommand { get; }
        public IRelayCommand BuscarCommand { get; }
        public IRelayCommand BuscarSatCommand { get; }
        public IRelayCommand DeletePdfCommand { get; }
        public IRelayCommand RefreshCommand { get; }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService();
            DocumentosMostrados = new ObservableCollection<DocumentoViewModel>();
            DocumentosSatMostrados = new ObservableCollection<DocumentoViewModel>();

            // Inicialización de comandos asíncronos
            CargarPdfCommand = new AsyncRelayCommand(() => CargarArchivoUniversalAsync(false));
            CargarPdfSatCommand = new AsyncRelayCommand(() => CargarArchivoUniversalAsync(true));

            BuscarCommand = new RelayCommand(Buscar);
            BuscarSatCommand = new RelayCommand(BuscarSat);
            DeletePdfCommand = new RelayCommand(DeletePdf, () => SelectedDocumento != null);
            RefreshCommand = new RelayCommand(RefreshView);

            NombreUsuarioLogueado = SessionService.CurrentUserName;
            LoadAllDocuments();
            CargarDatosNotariaDesdeBD();
        }

        private async Task CargarArchivoUniversalAsync(bool esParaSat)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos Soportados (*.pdf;*.csv;*.txt)|*.pdf;*.csv;*.txt"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string ruta = openFileDialog.FileName;

                // Liberamos la UI mientras se procesa el archivo
                await Task.Run(async () =>
                {
                    try
                    {
                        if (esParaSat && Path.GetExtension(ruta).ToLower() == ".csv")
                        {
                            await ProcesarCargaMasivaSatAsync(ruta);
                        }
                        else
                        {
                            await ProcesarDocumentoIndividualAsync(ruta, esParaSat ? "SAT" : "UIF");
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            MessageBox.Show($"Error al procesar: {ex.Message}"));
                    }
                });

                LoadAllDocuments();
            }
        }

        private async Task ProcesarCargaMasivaSatAsync(string ruta)
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
                    dt.Rows.Add(campos[1].Trim(), campos[2].Trim(), campos[3].Trim(), SessionService.CurrentUserId);
            }

            // Llama al método que creamos en el DataService
            await _databaseService.CargaMasivaListadoSatAsync(dt);

            string nombreArchivo = Path.GetFileName(ruta);
            string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados", nombreArchivo);
            Directory.CreateDirectory(Path.GetDirectoryName(destino));
            File.Copy(ruta, destino, true);

            await _databaseService.GuardarDocumentoAsync(nombreArchivo, destino, "LISTADO MASIVO", SessionService.CurrentUserId, "SAT");

            Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Carga masiva completada."));
        }

        private async Task ProcesarDocumentoIndividualAsync(string ruta, string modulo)
        {
            string extension = Path.GetExtension(ruta).ToLower();
            string contenido = extension == ".pdf" ? _pdfService.ExtraerTextoDePdf(ruta) : File.ReadAllText(ruta, Encoding.UTF8);

            string nombreArchivo = Path.GetFileName(ruta);
            string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados", nombreArchivo);
            Directory.CreateDirectory(Path.GetDirectoryName(destino));
            File.Copy(ruta, destino, true);

            await _databaseService.GuardarDocumentoAsync(nombreArchivo, destino, contenido, SessionService.CurrentUserId, modulo);
        }

        public void LoadAllDocuments()
        {
            var docsUif = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "UIF");
            var docsSat = _databaseService.GetDocumentsByUser(SessionService.CurrentUserId, "SAT");

            Application.Current.Dispatcher.Invoke(() =>
            {
                DocumentosMostrados.Clear();
                foreach (var d in docsUif) DocumentosMostrados.Add(new DocumentoViewModel(d));

                DocumentosSatMostrados.Clear();
                foreach (var d in docsSat) DocumentosSatMostrados.Add(new DocumentoViewModel(d));
            });
        }

        // --- MÉTODOS DE APOYO (Mantienen tu lógica original) ---
        private void Buscar() { /* Tu lógica de búsqueda UIF */ }
        private void BuscarSat() { /* Tu lógica de búsqueda SAT */ }
        private void CargarDatosNotariaDesdeBD() { /* Tu lógica de carga */ }
        private void RefreshView() { LoadAllDocuments(); }
        private void DeletePdf() { /* Tu lógica de borrado */ }
        private void UpdateConstanciaButtonStates(bool r) { /* Tu lógica de botones */ }
    }
}