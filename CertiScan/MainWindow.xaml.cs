using CertiScan.ViewModels;
using System.Windows;
using System.Windows.Controls;
using CertiScan.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel; // Necesario para detectar el cambio de propiedades

namespace CertiScan
{
    public partial class MainWindow : Window
    {
        private bool isSidebarOpen = false;
        private MainViewModel _viewModel; // Guardamos una referencia al ViewModel

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // SUSCRIPCIÓN AL ESTADO DE CARGA (Punto 3)
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.IsBusy))
                {
                    bool ocupado = _viewModel.IsBusy;

                    // Controlamos la visibilidad de los elementos que agregamos al XAML
                    MainProgressBar.Visibility = ocupado ? Visibility.Visible : Visibility.Collapsed;
                    TxtCargando.Visibility = ocupado ? Visibility.Visible : Visibility.Collapsed;

                    // Bloqueamos la interacción con la ventana mientras procesa para evitar errores
                    this.IsHitTestVisible = !ocupado;
                    this.Opacity = ocupado ? 0.8 : 1.0;
                }
            };
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
            // Validación crítica: Si los componentes aún no cargan, salimos del método
            if (TxtTituloModulo == null || ViewUIF == null || ViewSAT == null)
                return;

            if (BtnUIF.IsChecked == true)
            {
                TxtTituloModulo.Text = " | BÚSQUEDA - UNIDAD DE INTELIGENCIA FINANCIERA";
                if (ViewUIF != null) ViewUIF.Visibility = Visibility.Visible;
                if (ViewSAT != null) ViewSAT.Visibility = Visibility.Collapsed;
            }
            else if (BtnSAT.IsChecked == true)
            {
                TxtTituloModulo.Text = " | VERIFICACIÓN - ARTÍCULOS 69, 69-B y 69-B BIS";
                if (ViewUIF != null) ViewUIF.Visibility = Visibility.Collapsed;
                if (ViewSAT != null) ViewSAT.Visibility = Visibility.Visible;
            }
        }

        // =======================
        // HISTORIAL MÓDULO UIF
        // =======================
        private void VerHistorial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HistoryWindow ventanaHistorial = new HistoryWindow("UIF");
                ventanaHistorial.Owner = this;

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

        // ========================
        // HISTORIAL MÓDULO SAT
        // ========================
        private void VerHistorialSat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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