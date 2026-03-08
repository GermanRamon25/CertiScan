using CertiScan.Models;
using CertiScan.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Collections.Generic;

namespace CertiScan.ViewModels
{
    public class HistoryViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;
        // NUEVO: Agregamos el servicio del SAT
        private readonly PdfSatService _pdfSatService;

        public List<string> NombresArchivosActuales { get; set; } = new List<string>();
        public ObservableCollection<BusquedaHistorial> HistorialBusquedas { get; set; }

        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                SetProperty(ref _filterText, value);
                ApplyFilter();
            }
        }

        private ObservableCollection<BusquedaHistorial> _filteredHistorial;
        public ObservableCollection<BusquedaHistorial> FilteredHistorial
        {
            get => _filteredHistorial;
            set => SetProperty(ref _filteredHistorial, value);
        }

        public IRelayCommand<BusquedaHistorial> RegenerateCertificateCommand { get; }

        public HistoryViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService();
            // Inicializamos el servicio SAT
            _pdfSatService = new PdfSatService();

            HistorialBusquedas = new ObservableCollection<BusquedaHistorial>();
            FilteredHistorial = new ObservableCollection<BusquedaHistorial>();

            RegenerateCertificateCommand = new RelayCommand<BusquedaHistorial>(RegenerateCertificate);

            LoadHistory();
        }

        public void LoadHistory()
        {
            HistorialBusquedas.Clear();
            var user = SessionService.UsuarioLogueado;

            if (user != null)
            {
                var historyItems = _databaseService.GetSearchHistory(user.Id, user.NombreUsuario);

                foreach (var item in historyItems)
                {
                    HistorialBusquedas.Add(item);
                }
            }
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                FilteredHistorial = new ObservableCollection<BusquedaHistorial>(HistorialBusquedas);
            }
            else
            {
                var lowerFilter = FilterText.ToLower();
                var filtered = HistorialBusquedas.Where(h =>
                    (h.TerminoBuscado != null && h.TerminoBuscado.ToLower().Contains(lowerFilter)) ||
                    h.FechaCarga.ToString("dd/MM/yyyy").Contains(lowerFilter)
                );
                FilteredHistorial = new ObservableCollection<BusquedaHistorial>(filtered);
            }
        }

        private void RegenerateCertificate(BusquedaHistorial historyItem)
        {
            if (historyItem == null) return;

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), $"CertiScan_ReGen_{Guid.NewGuid()}.pdf");

                // Usamos la lista de archivos que pasamos desde la ventana principal
                List<string> nombresArchivos = NombresArchivosActuales;
                if (nombresArchivos == null || nombresArchivos.Count == 0)
                {
                    nombresArchivos = new List<string> { "Consulta realizada desde el historial del sistema" };
                }

                var infoDB = _databaseService.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);
                var datosParaPdf = new DatosNotaria
                {
                    NombreNotario = infoDB?.NombreNotario ?? "No Configurado",
                    NumeroNotaria = infoDB?.NumeroNotaria ?? "0",
                    DireccionCompleta = infoDB?.Direccion ?? "No configurada",
                    DatosContacto = $"Tel: {infoDB?.Telefono}"
                };

                // --- LÓGICA DE DISTINCIÓN ENTRE UIF Y SAT ---
                if (historyItem.TerminoBuscado.StartsWith("SAT: "))
                {
                    // Es un reporte del SAT
                    string rfcLimpio = historyItem.TerminoBuscado.Replace("SAT: ", "");
                    // En el SAT, "esLimpio" es lo opuesto a "ResultadoEncontrado"
                    _pdfSatService.GenerarReporteSat(tempPath, rfcLimpio, !historyItem.ResultadoEncontrado, nombresArchivos, datosParaPdf);
                }
                else
                {
                    // Es una constancia de la UIF
                    bool esAprobatoria = !historyItem.ResultadoEncontrado;
                    _pdfService.GenerarConstancia(tempPath, historyItem.TerminoBuscado, esAprobatoria, nombresArchivos, datosParaPdf);
                }

                var viewer = new PdfViewerWindow(tempPath, historyItem.TerminoBuscado);
                viewer.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al regenerar: {ex.Message}");
            }
        }
    }
}