using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CertiScan.ViewModels;

namespace CertiScan
{
    /// <summary>
    /// Lógica de interacción para HistoryWindow.xaml
    /// </summary>
    public partial class HistoryWindow : Window
    {
        // Constructor modificado para recibir el módulo
        public HistoryWindow(string modulo)
        {
            InitializeComponent();

            // Creamos el ViewModel y le pedimos que cargue los datos del módulo enviado
            var viewModel = new HistoryViewModel();
            viewModel.LoadHistory(modulo);

            this.DataContext = viewModel;
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ExportarCSV_Click(object sender, RoutedEventArgs e)
        {
            var datos = dgHistorial.ItemsSource as System.Collections.IEnumerable;

            if (datos == null) return;

            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Historial_CertiScan_{DateTime.Now:ddMMyyyy_HHmm}.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();

                    // ESTA LÍNEA ES LA CLAVE: Le dice a Excel qué separador usar
                    sb.AppendLine("sep=;");

                    // Encabezados
                    sb.AppendLine("Fecha;Usuario;Termino;Resultado");

                    foreach (var item in datos)
                    {
                        dynamic row = item;

                        // Aseguramos que no haya nulos y limpiamos caracteres que rompan el CSV
                        string fecha = row.FechaCarga.ToString("dd/MM/yyyy HH:mm");
                        string usuario = (row.NombreUsuario ?? "").ToString().Replace(";", ",");
                        string termino = (row.TerminoBuscado ?? "").ToString().Replace(";", ",");
                        string resultado = (row.ResultadoTexto ?? "").ToString().Replace(";", ",");

                        sb.AppendLine($"{fecha};{usuario};{termino};{resultado}");
                    }

                    // Guardamos con codificación UTF8 normal
                    System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);

                    MessageBox.Show("Reporte generado con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al exportar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}