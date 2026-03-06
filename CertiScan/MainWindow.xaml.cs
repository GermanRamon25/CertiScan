using CertiScan.ViewModels;
using System.Windows;
using System.Windows.Controls;
using CertiScan.Services;
using System;

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
        // MÉTODO DE HISTORIAL CORREGIDO
        // ============================================================
        private void VerHistorial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Crear ventana y ViewModel
                HistoryWindow ventanaHistorial = new HistoryWindow();
                ventanaHistorial.Owner = this;
                var historyVm = new HistoryViewModel();

                // 2. Traer datos frescos de la base de datos
                var db = new DatabaseService();
                var datos = db.GetSearchHistory(SessionService.CurrentUserId, SessionService.CurrentUserName);

                // 3. Llenar la lista "Historial" (así se llama en tu HistoryViewModel)
                if (historyVm.HistorialBusquedas != null)
                {
                    historyVm.HistorialBusquedas.Clear();
                    if (datos != null)
                    {
                        foreach (var item in datos)
                        {
                            historyVm.HistorialBusquedas.Add(item);
                        }
                    }
                }

                // 4. Conectar la ventana con los datos y abrirla
                ventanaHistorial.DataContext = historyVm;
                ventanaHistorial.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar historial: " + ex.Message);
            }
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