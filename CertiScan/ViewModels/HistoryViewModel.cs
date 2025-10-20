using CertiScan.Models;
using CertiScan.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace CertiScan.ViewModels
{
    public class HistoryViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;

        public ObservableCollection<BusquedaHistorial> Historial { get; set; }

        public IRelayCommand<BusquedaHistorial> RegenerateCertificateCommand { get; }

        public HistoryViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService();
            Historial = new ObservableCollection<BusquedaHistorial>();

            RegenerateCertificateCommand = new RelayCommand<BusquedaHistorial>(RegenerateCertificate);

            LoadHistory();
        }

        private void LoadHistory()
        {
            Historial.Clear();
            var historyItems = _databaseService.GetSearchHistory();
            foreach (var item in historyItems)
            {
                Historial.Add(item);
            }
        }


        // --- INICIO DE LA MODIFICACIÓN ---
        private void RegenerateCertificate(BusquedaHistorial historyItem)
        {
            if (historyItem == null) return;

            try
            {
                string tempFileName = $"Constancia_{historyItem.TerminoBuscado.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                bool esAprobatoria = !historyItem.ResultadoEncontrado;

                // AHORA LLAMAMOS AL MÉTODO CON 4 PARÁMETROS, INCLUYENDO LA FECHA ORIGINAL
                _pdfService.GenerarConstancia(tempFilePath, historyItem.TerminoBuscado, esAprobatoria, historyItem.FechaBusqueda);

                var viewer = new PdfViewerWindow(tempFilePath);
                viewer.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar la constancia: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // --- FIN DE LA MODIFICACIÓN ---
    }
}