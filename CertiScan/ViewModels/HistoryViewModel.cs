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
        private readonly PdfService _pdfService; // Se añade el servicio de PDF

        public ObservableCollection<BusquedaHistorial> Historial { get; set; }

        // --- INICIO DE LA MODIFICACIÓN ---
        public IRelayCommand<BusquedaHistorial> RegenerateCertificateCommand { get; }
        // --- FIN DE LA MODIFICACIÓN ---

        public HistoryViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService(); // Se inicializa el servicio de PDF
            Historial = new ObservableCollection<BusquedaHistorial>();

            // Se inicializa el nuevo comando
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
        // Nuevo método que se ejecuta al presionar el botón
        private void RegenerateCertificate(BusquedaHistorial historyItem)
        {
            if (historyItem == null) return;

            try
            {
                // Se reutiliza la lógica de MainViewModel para generar la constancia
                string tempFileName = $"Constancia_{historyItem.TerminoBuscado.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                // Determina si la constancia es aprobatoria basándose en el resultado guardado
                bool esAprobatoria = !historyItem.ResultadoEncontrado;

                // Genera el PDF
                _pdfService.GenerarConstancia(tempFilePath, historyItem.TerminoBuscado, esAprobatoria);

                // Muestra el visor de PDF
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