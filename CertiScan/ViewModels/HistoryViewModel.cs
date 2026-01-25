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

        // Lista completa desde la base de datos
        public ObservableCollection<BusquedaHistorial> Historial { get; set; }

        // Propiedades para el buscador dinámico
        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                SetProperty(ref _filterText, value);
                ApplyFilter(); // Filtra cada vez que el usuario escribe
            }
        }

        // Esta es la lista que se enlaza al DataGrid: ItemsSource="{Binding FilteredHistorial}"
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

            Historial = new ObservableCollection<BusquedaHistorial>();
            FilteredHistorial = new ObservableCollection<BusquedaHistorial>();

            RegenerateCertificateCommand = new RelayCommand<BusquedaHistorial>(RegenerateCertificate);

            LoadHistory();
        }

        private void LoadHistory()
        {
            Historial.Clear();
            var user = SessionService.UsuarioLogueado;

            if (user != null)
            {
                var historyItems = _databaseService.GetSearchHistory(user.Id, user.NombreUsuario);

                foreach (var item in historyItems)
                {
                    Historial.Add(item);
                }
            }

            // Al cargar, mostrar toda la lista inicialmente
            ApplyFilter();
        }

        // Lógica del buscador
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
                string tempFileName = $"Constancia_{historyItem.TerminoBuscado.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                bool esAprobatoria = !historyItem.ResultadoEncontrado;
                List<string> nombresArchivos = new List<string>();

                DatosNotaria datosParaPdf = null;

                if (SessionService.UsuarioLogueado != null)
                {
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