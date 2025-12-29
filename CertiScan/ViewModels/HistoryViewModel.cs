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

        private void RegenerateCertificate(BusquedaHistorial historyItem)
        {
            if (historyItem == null) return;

            try
            {
                string tempFileName = $"Constancia_{historyItem.TerminoBuscado.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                bool esAprobatoria = !historyItem.ResultadoEncontrado;

                // 1. Necesitamos recuperar la lista de archivos encontrados para esa búsqueda desde la DB
                // o pasar una lista vacía si el historial no la guarda.
                List<string> nombresArchivos = new List<string>();

                // 2. IMPORTANTE: Como estamos en Historial, usamos datos por defecto 
                // o podrías implementar una lógica para recordar qué notario la generó.
                var datosDefault = new DatosNotaria
                {
                    NombreNotario = "LIC. JAIME HUMBERTO CECEÑA IMPERIAL",
                    NumeroNotaria = "143",
                    DireccionCompleta = "BLVD. JUAN DE DIOS BÁTIZ NO. 86-7 ORIENTE, LOS MOCHIS, SINALOA",
                    DatosContacto = "Tel: (668) 815 6780 | notario143jc@gmail.com"
                };

                // 3. LLAMADA CORREGIDA: Ajustada a la nueva firma de PdfService que definimos antes
                _pdfService.GenerarConstancia(
                    tempFilePath,
                    historyItem.TerminoBuscado,
                    esAprobatoria,
                    nombresArchivos,
                    datosDefault
                );

                var viewer = new PdfViewerWindow(tempFilePath);
                viewer.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar la constancia: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}