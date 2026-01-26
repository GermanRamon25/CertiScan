using CertiScan.ViewModels;
using System.Windows;
using CertiScan.Services;

namespace CertiScan
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml corregida.
    /// Se eliminó la dependencia de WebView2 para usar visualización de texto nativa.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Conecta la Vista con el ViewModel
            var viewModel = new MainViewModel();
            DataContext = viewModel;

            // Al usar TextBlock con Binding en el XAML, ya no es necesario 
            // escuchar PropertyChanged ni llamar a métodos de actualización manual.
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Cierra la sesión y regresa al Login
            SessionService.Logout();
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void ConfigurarNotaria_Click(object sender, RoutedEventArgs e)
        {
            // Abre la ventana de configuración de la notaría
            NotariaWindow ventanaNotaria = new NotariaWindow();
            ventanaNotaria.Owner = this;
            ventanaNotaria.ShowDialog();
        }
    }
}