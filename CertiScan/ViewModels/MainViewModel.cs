using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CertiScan.Models; // Asegúrate que el namespace apunte a tus Modelos
using CertiScan.Services; // Asegúrate que el namespace apunte a tus Servicios

namespace CertiScan.ViewModels
{
    // Heredamos de ObservableObject para poder notificar a la UI de los cambios
    public class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly PdfService _pdfService;

        // --- Propiedad 'TerminoBusqueda' implementada de forma explícita ---
        private string _terminoBusqueda = string.Empty;
        public string TerminoBusqueda
        {
            get => _terminoBusqueda;
            set => SetProperty(ref _terminoBusqueda, value); // Notifica a la UI cuando cambia
        }

        public ObservableCollection<Documento> ResultadosBusqueda { get; set; }

        // --- Comandos implementados de forma explícita ---
        public IRelayCommand CargarPdfCommand { get; }
        public IRelayCommand BuscarCommand { get; }
        public IRelayCommand<bool> GenerarConstanciaCommand { get; }

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            _pdfService = new PdfService();
            ResultadosBusqueda = new ObservableCollection<Documento>();

            // Inicializamos los comandos, apuntando a los métodos que deben ejecutar
            CargarPdfCommand = new RelayCommand(CargarPdf);
            BuscarCommand = new RelayCommand(Buscar);
            GenerarConstanciaCommand = new RelayCommand<bool>(GenerarConstancia);
        }

        private void CargarPdf()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos PDF (.pdf)|.pdf",
                Title = "Seleccionar archivo PDF para cargar"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string carpetaDestino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocumentosAlmacenados");
                    Directory.CreateDirectory(carpetaDestino);
                    string nombreArchivo = Path.GetFileName(openFileDialog.FileName);
                    string rutaDestino = Path.Combine(carpetaDestino, nombreArchivo);
                    File.Copy(openFileDialog.FileName, rutaDestino, true);
                    string contenido = _pdfService.ExtraerTextoDePdf(rutaDestino);
                    _databaseService.GuardarDocumento(nombreArchivo, rutaDestino, contenido);
                    MessageBox.Show("¡PDF cargado y procesado exitosamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al cargar el PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Buscar()
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                MessageBox.Show("Por favor, ingrese un término de búsqueda.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var resultados = _databaseService.BuscarTermino(TerminoBusqueda);
                ResultadosBusqueda.Clear();
                foreach (var doc in resultados)
                {
                    ResultadosBusqueda.Add(doc);
                }
                _databaseService.RegistrarBusqueda(TerminoBusqueda, resultados.Count > 0);
                if (resultados.Count == 0)
                {
                    MessageBox.Show("No se encontraron coincidencias para el término buscado.", "Búsqueda Finalizada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al realizar la búsqueda: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerarConstancia(bool esAprobatoria)
        {
            if (string.IsNullOrWhiteSpace(TerminoBusqueda))
            {
                MessageBox.Show("Debe realizar una búsqueda primero para generar una constancia.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Archivo PDF (.pdf)|.pdf",
                Title = "Guardar Constancia",
                FileName = $"Constancia_{TerminoBusqueda.Replace(" ", "")}{DateTime.Now:yyyyMMdd}.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _pdfService.GenerarConstancia(saveFileDialog.FileName, TerminoBusqueda, esAprobatoria);
                    MessageBox.Show($"Constancia generada exitosamente en:\n{saveFileDialog.FileName}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al generar la constancia: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}