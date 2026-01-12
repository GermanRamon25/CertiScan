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
        public HistoryWindow()
        {
            InitializeComponent();
            DataContext = new HistoryViewModel();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        // Añade esto dentro de la clase HistoryWindow
        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Esto cerrará la ventana cuando hagas clic en la X
        }
    }
}
