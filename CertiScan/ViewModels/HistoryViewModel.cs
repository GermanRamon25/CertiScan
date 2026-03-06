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

        // CAMBIO CLAVE: El nombre debe ser HistorialBusquedas para que MainWindow lo encuentre
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

            // INICIALIZACIÓN (Esto quita el error del .Clear() en MainWindow)
            HistorialBusquedas = new ObservableCollection<BusquedaHistorial>();
            FilteredHistorial = new ObservableCollection<BusquedaHistorial>();

            RegenerateCertificateCommand = new RelayCommand<BusquedaHistorial>(RegenerateCertificate);

            // Carga inicial al abrir la ventana
            LoadHistory();
        }

        public void LoadHistory()
        {
            // Ahora .Clear() no dará error porque la lista ya se creó en el constructor arriba
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
                bool esAprobatoria = !historyItem.ResultadoEncontrado;
                List<string> nombresArchivos = new List<string>();

                var infoDB = _databaseService.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);
                var datosParaPdf = new DatosNotaria
                {
                    NombreNotario = infoDB?.NombreNotario ?? "No Configurado",
                    NumeroNotaria = infoDB?.NumeroNotaria ?? "0",
                    DireccionCompleta = infoDB?.Direccion ?? "No configurada",
                    DatosContacto = $"Tel: {infoDB?.Telefono}"
                };

                _pdfService.GenerarConstancia(tempPath, historyItem.TerminoBuscado, esAprobatoria, nombresArchivos, datosParaPdf);

                // --- NOMBRE AUTOMÁTICO AL GUARDAR ---
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