using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CertiScan.Services;
using System;
using System.IO;
using System.Text.RegularExpressions;
using CertiScan.Models;
using CertiScan.ViewModels;
namespace CertiScan.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;

        // Propiedad para que el WebView2 sepa qué archivo cargar
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

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        ClearSearchHighlights();
                        if (SelectedDocumento != null)
                            LoadPdfContent(SelectedDocumento.Id);
                    }
                    else if (SelectedDocumento != null)
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
        private string _nombreNotarioInput;
        public string NombreNotarioInput { get => _nombreNotarioInput; set => SetProperty(ref _nombreNotarioInput, value); }

        private string _numeroNotariaInput;
        public string NumeroNotariaInput { get => _numeroNotariaInput; set => SetProperty(ref _numeroNotariaInput, value); }

        private string _direccionInput;
        public string DireccionInput { get => _direccionInput; set => SetProperty(ref _direccionInput, value); }

        private string _contactoInput;
        public string ContactoInput { get => _contactoInput; set => SetProperty(ref _contactoInput, value); }

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
                        ContenidoDocumento = new FlowDocument();
                        RutaPdfActual = null;
                    }
                }
            }
        }

        private FlowDocument _contenidoDocumento;
        public FlowDocument ContenidoDocumento { get => _contenidoDocumento; set => SetProperty(ref _contenidoDocumento, value); }

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
                MessageBox.Show("Por favor, ingrese un nombre o término para buscar.", "Campo Vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            {
                MessageBox.Show($"¡COINCIDENCIA ENCONTRADA!\n\nSe han detectado {resultados.Count} coincidencias para '{TerminoBusqueda}'.", "Alerta de Seguridad", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show($"Búsqueda finalizada. No se encontraron coincidencias para '{TerminoBusqueda}'.", "Resultado de Búsqueda", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

        private void ClearSearchHighlights() { foreach (var doc in DocumentosMostrados) doc.IsSearchResult = false; }

        private void GenerarConstancia(bool parametro)
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda)) return;

            var infoManual = new DatosNotaria
            {
                NombreNotario = string.IsNullOrWhiteSpace(NombreNotarioInput) ? "NOMBRE NO ESPECIFICADO" : NombreNotarioInput,
                NumeroNotaria = string.IsNullOrWhiteSpace(NumeroNotariaInput) ? "0" : NumeroNotariaInput,
                DireccionCompleta = string.IsNullOrWhiteSpace(DireccionInput) ? "SIN DIRECCIÓN REGISTRADA" : DireccionInput,
                DatosContacto = string.IsNullOrWhiteSpace(ContactoInput) ? "" : ContactoInput
            };

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), $"Constancia_{TerminoBusqueda.Replace(" ", "_")}.pdf");
                _pdfService.GenerarConstancia(tempPath, TerminoBusqueda, !ResultadoEncontrado, _nombresArchivosEncontrados, infoManual);
                new PdfViewerWindow(tempPath).Show();
                RefreshView();
                CargarDatosNotariaDesdeBD();
            }
            catch (Exception ex) { MessageBox.Show("Error PDF: " + ex.Message); }
        }

        private bool CanDeletePdf() => SelectedDocumento != null;

        private void DeletePdf()
        {
            if (!CanDeletePdf()) return;
            var result = MessageBox.Show($"¿Estás seguro de que quieres eliminar '{SelectedDocumento.NombreArchivo}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                string filePath = _databaseService.DeleteDocument(SelectedDocumento.Id);
                if (File.Exists(filePath)) File.Delete(filePath);
                DocumentosMostrados.Remove(SelectedDocumento);
                SelectedDocumento = null;
            }
        }

        // Dentro de MainViewModel.cs, modifica el método LoadAllDocuments
        private void LoadAllDocuments()
        {
            DocumentosMostrados.Clear();
            var documentosBase = _databaseService.GetAllDocuments();

            foreach (var doc in documentosBase)
            {
                // Convertimos el modelo simple al ViewModel que entiende la interfaz
                DocumentosMostrados.Add(new DocumentoViewModel(doc));
            }
        }

        private void CargarPdf()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Filter = "Archivos PDF|*.pdf", Multiselect = true };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string ruta in openFileDialog.FileNames)
                {
                    try
                    {
                        string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados", Path.GetFileName(ruta));
                        Directory.CreateDirectory(Path.GetDirectoryName(destino));
                        File.Copy(ruta, destino, true);
                        _databaseService.GuardarDocumento(Path.GetFileName(ruta), destino, _pdfService.ExtraerTextoDePdf(destino));
                    }
                    catch (Exception ex) { MessageBox.Show("Error al cargar: " + ex.Message); }
                }
                RefreshView();
            }
        }

        private void LoadPdfContent(int docId)
        {
            try { ContenidoDocumento = CreateHighlightedFlowDocument(_databaseService.GetDocumentoContent(docId), TerminoBusqueda); }
            catch { ContenidoDocumento = new FlowDocument(); }
        }

        private void ShowHistory() => new HistoryWindow().Show();

        private void RefreshView()
        {
            TerminoBusqueda = string.Empty;
            SelectedDocumento = null;
            LoadAllDocuments();
            ContenidoDocumento = new FlowDocument();
            RutaPdfActual = null;
        }

        private FlowDocument CreateHighlightedFlowDocument(string text, string searchTerm)
        {
            var flowDoc = new FlowDocument();
            var p = new Paragraph();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                p.Inlines.Add(new Run(text));
            }
            else
            {
                string pattern = Regex.Escape(searchTerm);
                string[] parts = Regex.Split(text, $"({pattern})", RegexOptions.IgnoreCase);

                foreach (var part in parts)
                {
                    if (part.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        p.Inlines.Add(new Run(part) { Background = Brushes.Yellow, FontWeight = FontWeights.Bold });
                    }
                    else
                    {
                        p.Inlines.Add(new Run(part));
                    }
                }
            }
            flowDoc.Blocks.Add(p);
            return flowDoc;
        }
    }
}