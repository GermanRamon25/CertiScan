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
    }
}