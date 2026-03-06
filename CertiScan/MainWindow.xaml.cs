using CertiScan.ViewModels;
using System.Windows;
using System.Windows.Controls;
using CertiScan.Services;

namespace CertiScan
{
    public partial class MainWindow : Window
    {
        private bool isSidebarOpen = false;

        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainViewModel();
            DataContext = viewModel;
        }

        private void SidebarToggle_Click(object sender, RoutedEventArgs e)
        {
            if (isSidebarOpen)
            {
                Sidebar.Width = 0;
                isSidebarOpen = false;
            }
            else
            {
                Sidebar.Width = 220;
                isSidebarOpen = true;
            }
        }

        private void Menu_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton == null || ViewUIF == null || ViewSAT == null) return;

            if (radioButton.Name == "BtnUIF")
            {
                ViewUIF.Visibility = Visibility.Visible;
                ViewSAT.Visibility = Visibility.Collapsed;
            }
            else if (radioButton.Name == "BtnSAT")
            {
                ViewUIF.Visibility = Visibility.Collapsed;
                ViewSAT.Visibility = Visibility.Visible;
            }
        }

        // ============================================================
        // ESTO ES LO QUE FALTABA PARA QUE EL BOTÓN FUNCIONARA
        // ============================================================
        private void VerHistorial_Click(object sender, RoutedEventArgs e)
        {
            // 1. Creamos la ventana de historial
            HistoryWindow ventanaHistorial = new HistoryWindow();
            ventanaHistorial.Owner = this;

            // 2. Creamos su ViewModel y cargamos los datos reales de la base de datos
            var historyVm = new HistoryViewModel();

            // Usamos el servicio de base de datos para traer la información
            var db = new DatabaseService();
            var datos = db.GetSearchHistory(SessionService.CurrentUserId, SessionService.CurrentUserName);

            // 3. Limpiamos y llenamos la lista que se mostrará en la tabla
            historyVm.HistorialBusquedas.Clear();
            foreach (var item in datos)
            {
                historyVm.HistorialBusquedas.Add(item);
            }

            // 4. Conectamos la ventana con estos datos y la abrimos
            ventanaHistorial.DataContext = historyVm;
            ventanaHistorial.ShowDialog();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            SessionService.Logout();
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ConfigurarNotaria_Click(object sender, RoutedEventArgs e)
        {
            NotariaWindow ventanaNotaria = new NotariaWindow();
            ventanaNotaria.Owner = this;
            ventanaNotaria.ShowDialog();
        }
    }
}