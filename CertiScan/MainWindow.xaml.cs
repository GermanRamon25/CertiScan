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
                HistoryWindow ventanaHistorial = new HistoryWindow();
                ventanaHistorial.Owner = this;
                var historyVm = new HistoryViewModel();

                if (DataContext is MainViewModel mainVm)
                {
                    historyVm.NombresArchivosActuales = mainVm.DocumentosMostrados.Select(d => d.NombreArchivo).ToList();
                }

                var db = new DatabaseService();
                // CORRECCIÓN: Se agrega el parámetro "UIF"
                var datos = db.GetSearchHistory(SessionService.CurrentUserId, SessionService.CurrentUserName, "UIF");

                if (historyVm.HistorialBusquedas != null && datos != null)
                {
                    historyVm.HistorialBusquedas.Clear();
                    foreach (var item in datos)
                    {
                        // Ya no necesitas filtrar manualmente por longitud, la BD ya lo trae separado
                        historyVm.HistorialBusquedas.Add(item);
                    }
                }

                ventanaHistorial.Title = "Historial - Búsqueda UIF";
                ventanaHistorial.DataContext = historyVm;
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
                HistoryWindow ventanaHistorial = new HistoryWindow();
                ventanaHistorial.Owner = this;
                var historyVm = new HistoryViewModel();

                if (DataContext is MainViewModel mainVm)
                {
                    historyVm.NombresArchivosActuales = mainVm.DocumentosSatMostrados.Select(d => d.NombreArchivo).ToList();
                }

                var db = new DatabaseService();
                // CORRECCIÓN: Se agrega el parámetro "SAT"
                var datos = db.GetSearchHistory(SessionService.CurrentUserId, SessionService.CurrentUserName, "SAT");

                if (historyVm.HistorialBusquedas != null && datos != null)
                {
                    historyVm.HistorialBusquedas.Clear();
                    foreach (var item in datos)
                    {
                        // Ya no necesitas filtrar manualmente por longitud
                        historyVm.HistorialBusquedas.Add(item);
                    }
                }

                ventanaHistorial.Title = "Historial - Verificación SAT";
                ventanaHistorial.DataContext = historyVm;
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