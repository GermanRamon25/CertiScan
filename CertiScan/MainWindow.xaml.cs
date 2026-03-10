using CertiScan.ViewModels;
using System.Windows;
using System.Windows.Controls;
using CertiScan.Services;
using System;
using System.Linq;
using System.Collections.Generic;

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
        // HISTORIAL MÓDULO UIF
        // ============================================================
        private void VerHistorial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // CORRECCIÓN: Ahora pasamos "UIF" al constructor de la ventana
                HistoryWindow ventanaHistorial = new HistoryWindow("UIF");
                ventanaHistorial.Owner = this;

                // El DataContext ya se asigna dentro del constructor de HistoryWindow, 
                // pero si necesitas pasar los archivos actuales lo hacemos así:
                if (ventanaHistorial.DataContext is HistoryViewModel historyVm && DataContext is MainViewModel mainVm)
                {
                    historyVm.NombresArchivosActuales = mainVm.DocumentosMostrados.Select(d => d.NombreArchivo).ToList();
                }

                ventanaHistorial.Title = "Historial - Búsqueda UIF";
                ventanaHistorial.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar historial UIF: " + ex.Message);
            }
        }

        // ============================================================
        // HISTORIAL MÓDULO SAT
        // ============================================================
        private void VerHistorialSat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // CORRECCIÓN: Ahora pasamos "SAT" al constructor de la ventana
                HistoryWindow ventanaHistorial = new HistoryWindow("SAT");
                ventanaHistorial.Owner = this;

                if (ventanaHistorial.DataContext is HistoryViewModel historyVm && DataContext is MainViewModel mainVm)
                {
                    historyVm.NombresArchivosActuales = mainVm.DocumentosSatMostrados.Select(d => d.NombreArchivo).ToList();
                }

                ventanaHistorial.Title = "Historial - Verificación SAT";
                ventanaHistorial.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar historial SAT: " + ex.Message);
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