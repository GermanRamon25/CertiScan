using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CertiScan.Services;
using System;
using System.IO;
using System.Text.RegularExpressions;
using CertiScan.Models;

namespace CertiScan.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;

        private string _rutaPdfActual;
        public string RutaPdfActual
        {
            get => _rutaPdfActual;
            set => SetProperty(ref _rutaPdfActual, value);
        }

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
                    UpdateConstanciaButtonStates(busquedaRealizada: false);

                    if (SelectedDocumento != null)
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
            set => SetProperty(ref _resultadoEncontrado, value);
        }

        private List<string> _nombresArchivosEncontrados = new List<string>();

        // Propiedades de Notaría
        public string NombreNotarioInput { get; set; }
        public string NumeroNotariaInput { get; set; }
        public string DireccionInput { get; set; }
        public string ContactoInput { get; set; }

        private bool _isAprobatoriaButtonEnabled = false;
        public bool IsAprobatoriaButtonEnabled { get => _isAprobatoriaButtonEnabled; set => SetProperty(ref _isAprobatoriaButtonEnabled, value); }

        private bool _isDenegadaButtonEnabled = false;
        public bool IsDenegadaButtonEnabled { get => _isDenegadaButtonEnabled; set => SetProperty(ref _isDenegadaButtonEnabled, value); }

        public ObservableCollection<DocumentoViewModel> DocumentosMostrados { get; set; }

        private DocumentoViewModel _selectedDocumento;
        public DocumentoViewModel SelectedDocumento
        {
            get => _selectedDocumento;
            set
            {
                if (SetProperty(ref _selectedDocumento, value))
                {
                    if (value != null)
                    {
                        LoadPdfContent(value.Id);
                        RutaPdfActual = value.RutaArchivo;
                    }
                    else
                    {
                        ContenidoDocumento = string.Empty; // Limpiamos el texto
                        RutaPdfActual = null;
                    }
                }
            }
        }

        // CAMBIO CLAVE: Cambiado de FlowDocument a string
        private string _contenidoDocumento;
        public string ContenidoDocumento
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
            CargarDatosNotariaDesdeBD();

            CargarPdfCommand = new RelayCommand(CargarPdf);
            BuscarCommand = new RelayCommand(Buscar);
            GenerarConstanciaCommand = new RelayCommand<bool>(GenerarConstancia);
            DeletePdfCommand = new RelayCommand(DeletePdf, CanDeletePdf);
            ShowHistoryCommand = new RelayCommand(ShowHistory);
            RefreshCommand = new RelayCommand(RefreshView);

            NombreUsuarioLogueado = SessionService.CurrentUserName;
            UpdateConstanciaButtonStates(busquedaRealizada: false);
        }

        private void CargarDatosNotariaDesdeBD()
        {
            if (SessionService.UsuarioLogueado != null)
            {
                var info = _databaseService.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);
                if (info != null)
                {
                    NombreNotarioInput = info.NombreNotario;
                    NumeroNotariaInput = info.NumeroNotaria;
                    DireccionInput = info.Direccion;
                    ContactoInput = $"Tel: {info.Telefono} | Email: {info.Email}";
                }
            }
        }

        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                MessageBox.Show("Por favor, ingrese un nombre para buscar.", "Campo Vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultados = _databaseService.BuscarTermino(TerminoBusqueda);
            bool encontrado = resultados.Count > 0;
            _nombresArchivosEncontrados = encontrado ? resultados.Select(d => d.NombreArchivo).ToList() : new List<string>();
            ResultadoEncontrado = encontrado;

            UpdateConstanciaButtonStates(busquedaRealizada: true);

            var ids = new HashSet<int>(resultados.Select(r => r.Id));
            foreach (var doc in DocumentosMostrados) doc.IsSearchResult = ids.Contains(doc.Id);

            if (SelectedDocumento != null) LoadPdfContent(SelectedDocumento.Id);

            _databaseService.RegistrarBusqueda(TerminoBusqueda, encontrado, SessionService.CurrentUserId);

            if (encontrado)
                MessageBox.Show($"¡COINCIDENCIA ENCONTRADA!", "Alerta", MessageBoxButton.OK, MessageBoxImage.Warning);
            else
                MessageBox.Show($"No se encontraron coincidencias.", "Resultado", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateConstanciaButtonStates(bool busquedaRealizada)
        {
            if (!busquedaRealizada || string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                IsAprobatoriaButtonEnabled = false;
                IsDenegadaButtonEnabled = false;
                return;
            }
            IsAprobatoriaButtonEnabled = !ResultadoEncontrado;
            IsDenegadaButtonEnabled = ResultadoEncontrado;
        }

        private void GenerarConstancia(bool parametro)
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;

            var infoManual = new DatosNotaria
            {
                NombreNotario = string.IsNullOrWhiteSpace(NombreNotarioInput) ? "NOMBRE NO ESPECIFICADO" : NombreNotarioInput,
                NumeroNotaria = string.IsNullOrWhiteSpace(NumeroNotariaInput) ? "0" : NumeroNotariaInput,
                DireccionCompleta = string.IsNullOrWhiteSpace(DireccionInput) ? "SIN DIRECCIÓN" : DireccionInput,
                DatosContacto = string.IsNullOrWhiteSpace(ContactoInput) ? "" : ContactoInput
            };

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), $"Constancia_{TerminoBusqueda.Replace(" ", "_")}.pdf");
                _pdfService.GenerarConstancia(tempPath, TerminoBusqueda, !ResultadoEncontrado, _nombresArchivosEncontrados, infoManual);
                new PdfViewerWindow(tempPath).Show();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private bool CanDeletePdf() => SelectedDocumento != null;

        private void DeletePdf()
        {
            if (!CanDeletePdf()) return;
            if (MessageBox.Show($"¿Eliminar '{SelectedDocumento.NombreArchivo}'?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _databaseService.DeleteDocument(SelectedDocumento.Id);
                DocumentosMostrados.Remove(SelectedDocumento);
                SelectedDocumento = null;
            }
        }

        private void LoadAllDocuments()
        {
            DocumentosMostrados.Clear();
            var documentosBase = _databaseService.GetAllDocuments();
            foreach (var doc in documentosBase) DocumentosMostrados.Add(new DocumentoViewModel(doc));
        }

        private void CargarPdf()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Filter = "Archivos PDF|*.pdf", Multiselect = true };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string ruta in openFileDialog.FileNames)
                {
                    string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados", Path.GetFileName(ruta));
                    Directory.CreateDirectory(Path.GetDirectoryName(destino));
                    File.Copy(ruta, destino, true);
                    _databaseService.GuardarDocumento(Path.GetFileName(ruta), destino, _pdfService.ExtraerTextoDePdf(destino));
                }
                LoadAllDocuments();
            }
        }

        private void LoadPdfContent(int docId)
        {
            try
            {
                // Cargamos el texto plano directamente
                ContenidoDocumento = _databaseService.GetDocumentoContent(docId);
            }
            catch { ContenidoDocumento = string.Empty; }
        }

        private void ShowHistory() => new HistoryWindow().Show();

        private void RefreshView()
        {
            TerminoBusqueda = string.Empty;
            SelectedDocumento = null;
            LoadAllDocuments();
            ContenidoDocumento = string.Empty;
            RutaPdfActual = null;
        }
    }
}