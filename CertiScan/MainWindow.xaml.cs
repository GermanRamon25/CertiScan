using CertiScan.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CertiScan.Services;
using Microsoft.Web.WebView2.Core;
using CertiScan.Models;



namespace CertiScan
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Conecta la Vista con el ViewModel
            var viewModel = new MainViewModel();
            DataContext = viewModel;

            // --- ESCUCHAR CAMBIOS DE SELECCIÓN PARA ACTUALIZAR EL PDF ---
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "SelectedDocumento" && viewModel.SelectedDocumento != null)
                {
                    ActualizarVisorPdf(viewModel.SelectedDocumento.RutaArchivo);
                }
            };

            // Inicializar el motor del navegador
            InicializarVisor();
        }

        private async void InicializarVisor()
        {
            try
            {
                // Espera a que el motor del navegador interno se cree
                await webView.EnsureCoreWebView2Async(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al inicializar el visor de PDF: " + ex.Message);
            }
        }

        private void ActualizarVisorPdf(string ruta)
        {
            if (webView != null && webView.CoreWebView2 != null && !string.IsNullOrEmpty(ruta))
            {
                webView.CoreWebView2.Navigate(new Uri(ruta).AbsoluteUri);
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