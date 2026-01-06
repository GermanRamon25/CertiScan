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
                List<string> nombresArchivos = new List<string>();

                // --- CORRECCIÓN AQUÍ: Recuperar datos reales de la base de datos ---
                DatosNotaria datosParaPdf = null;

                if (SessionService.UsuarioLogueado != null)
                {
                    // Buscamos la información de la notaría asociada al usuario actual
                    var infoDB = _databaseService.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);

                    if (infoDB != null)
                    {
                        datosParaPdf = new DatosNotaria
                        {
                            NombreNotario = infoDB.NombreNotario,
                            NumeroNotaria = infoDB.NumeroNotaria,
                            DireccionCompleta = infoDB.Direccion,
                            DatosContacto = $"Tel: {infoDB.Telefono} | Email: {infoDB.Email}"
                        };
                    }
                }

                // Si por alguna razón no se encuentran datos (ej. sesión expirada), 
                // usamos un objeto de advertencia en lugar de datos falsos
                if (datosParaPdf == null)
                {
                    datosParaPdf = new DatosNotaria
                    {
                        NombreNotario = "DATO NO CONFIGURADO",
                        NumeroNotaria = "0",
                        DireccionCompleta = "Favor de configurar en el menú Notaría",
                        DatosContacto = ""
                    };
                }

                // LLAMADA CORREGIDA: Usamos 'datosParaPdf' que trae la info real de la DB
                _pdfService.GenerarConstancia(
                    tempFilePath,
                    historyItem.TerminoBuscado,
                    esAprobatoria,
                    nombresArchivos,
                    datosParaPdf
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