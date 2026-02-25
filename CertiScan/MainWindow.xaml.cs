using CertiScan.ViewModels;
using System.Windows;
using System.Windows.Controls;
using CertiScan.Services;

namespace CertiScan
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// Gestiona el menú lateral, el cambio de vistas (UIF vs SAT) y la sesión.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Variable para controlar el estado del menú lateral
        private bool isSidebarOpen = false;

        public MainWindow()
        {
            InitializeComponent();

            // Conectar con el ViewModel (Lógica de negocio)
            var viewModel = new MainViewModel();
            DataContext = viewModel;
        }

        /// <summary>
        /// Abre o cierra el menú lateral (Sidebar) cambiando su ancho.
        /// </summary>
        private void SidebarToggle_Click(object sender, RoutedEventArgs e)
        {
            if (isSidebarOpen)
            {
                // CERRAR: Cambia el ancho a 0 para ocultarlo
                Sidebar.Width = 0;
                isSidebarOpen = false;
            }
            else
            {
                // ABRIR: Restaura el ancho original (ajusta 220 al tamaño que desees)
                Sidebar.Width = 220;
                isSidebarOpen = true;
            }
        }

        /// <summary>
        /// Maneja el cambio de pestañas/vistas desde el menú lateral.
        /// Alterna la visibilidad entre el panel de UIF y el panel del SAT.
        /// </summary>
        private void Menu_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;

            // Validación de seguridad por si los controles aún no cargan
            if (radioButton == null || ViewUIF == null || ViewSAT == null) return;

            // Verificamos qué botón se seleccionó mediante su nombre o Tag
            if (radioButton.Name == "BtnUIF")
            {
                // Mostrar UIF, Ocultar SAT
                ViewUIF.Visibility = Visibility.Visible;
                ViewSAT.Visibility = Visibility.Collapsed;
            }
            else if (radioButton.Name == "BtnSAT")
            {
                // Mostrar SAT, Ocultar UIF
                ViewUIF.Visibility = Visibility.Collapsed;
                ViewSAT.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Cierra la sesión actual y regresa a la ventana de Login.
        /// </summary>
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            SessionService.Logout();
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        /// <summary>
        /// Abre la ventana modal para configurar los datos de la notaría.
        /// </summary>
        private void ConfigurarNotaria_Click(object sender, RoutedEventArgs e)
        {
            NotariaWindow ventanaNotaria = new NotariaWindow();
            ventanaNotaria.Owner = this;
            ventanaNotaria.ShowDialog();
        }
    }
}