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

        public ObservableCollection<BusquedaHistorial> Historial { get; set; }

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

            // INICIALIZACIÓN CLAVE
            Historial = new ObservableCollection<BusquedaHistorial>();
            FilteredHistorial = new ObservableCollection<BusquedaHistorial>();

            RegenerateCertificateCommand = new RelayCommand<BusquedaHistorial>(RegenerateCertificate);

            LoadHistory();
        }

        public void LoadHistory()
        {
            Historial.Clear();
            var user = SessionService.UsuarioLogueado;

            if (user != null)
            {
                // Asegúrate de que el método en DatabaseService se llame GetSearchHistory
                var historyItems = _databaseService.GetSearchHistory(user.Id, user.NombreUsuario);

                foreach (var item in historyItems)
                {
                    Historial.Add(item);
                }
            }
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                FilteredHistorial = new ObservableCollection<BusquedaHistorial>(Historial);
            }
            else
            {
                var lowerFilter = FilterText.ToLower();
                var filtered = Historial.Where(h =>
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
                // Nombre temporal para el visor
                string tempPath = Path.Combine(Path.GetTempPath(), $"CertiScan_ReGen_{Guid.NewGuid()}.pdf");

                bool esAprobatoria = !historyItem.ResultadoEncontrado;

                // En el historial no tenemos la lista de archivos original, enviamos vacío o un texto genérico
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

                // --- CORRECCIÓN CLAVE PARA EL NOMBRE AUTOMÁTICO ---
                // Le pasamos el término buscado al constructor para que al guardar aparezca "Constancia_NOMBRE_FECHA"
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