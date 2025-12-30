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
using CertiScan.Services; // <-- AÑADIR ESTE USING

namespace CertiScan
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // Inicializa los controles que diseñaste en el XAML.
            InitializeComponent();

            // Conecta la Vista (esta ventana) con el ViewModel (la lógica).
            DataContext = new MainViewModel();
        }

        // --- INICIO DE LA MODIFICACIÓN ---
        // Método para manejar el clic del botón "Salir"
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Limpiar la sesión de usuario
            SessionService.Logout();

            // 2. Crear y mostrar la ventana de Login
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            // 3. Cerrar esta ventana principal (MainWindow)
            this.Close();
        }
        private void ConfigurarNotaria_Click(object sender, RoutedEventArgs e)
        {
            // Esto crea y muestra la ventana que estuvimos programando
            NotariaWindow ventanaNotaria = new NotariaWindow();
            ventanaNotaria.Owner = this; // Para que se centre respecto a la principal
            ventanaNotaria.ShowDialog(); // ShowDialog hace que sea una ventana emergente
        }

    }
}