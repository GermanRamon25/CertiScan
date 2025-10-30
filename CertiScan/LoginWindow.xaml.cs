using CertiScan.Services;
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

namespace CertiScan
{
    public partial class LoginWindow : Window
    {
        private readonly DatabaseService _databaseService;

        public LoginWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
        }

        // Método para sincronizar el texto del PasswordBox al TextBox (Mostrar)
        private void TogglePasswordCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            // 1. Copia la contraseña oculta al TextBox visible
            VisiblePasswordBox.Text = PasswordBox.Password;

            // 2. Muestra el TextBox y oculta el PasswordBox
            VisiblePasswordBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }

        // Método para sincronizar el texto del TextBox al PasswordBox (Ocultar)
        private void TogglePasswordCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            // 1. Copia el texto visible al PasswordBox
            PasswordBox.Password = VisiblePasswordBox.Text;

            // 2. Oculta el TextBox y muestra el PasswordBox
            VisiblePasswordBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
        }

        // Sincroniza el texto cuando el usuario escribe en el TextBox visible
        private void VisiblePasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Solo actualiza si el checkbox está marcado (es decir, si el VisiblePasswordBox está en uso)
            if (TogglePasswordCheckbox.IsChecked == true)
            {
                PasswordBox.Password = VisiblePasswordBox.Text;
            }
        }

        // --- INICIO DE LA MODIFICACIÓN ---
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Text = ""; // Limpia errores previos
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            try
            {
                if (_databaseService.ValidateUser(username, password))
                {
                    // --- INICIO DE LA MODIFICACIÓN ---
                    // Se obtiene la información completa del usuario desde la base de datos.
                    var user = _databaseService.GetUserByUsername(username);
                    if (user != null)
                    {
                        // Se guardan el ID y el nombre del usuario en el servicio de sesión.
                        SessionService.Login(user.Id, user.NombreUsuario);
                    }
                    // --- FIN DE LA MODIFICACIÓN ---

                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    ErrorMessage.Text = "Usuario o contraseña incorrectos.";
                }
            }
            catch (Exception ex)
            {
                // ESTA ES LA CORRECCIÓN:
                // Si la conexión falla (por App.config mal, servidor apagado, etc.)
                // el error se mostrará aquí en lugar de cerrar la app.
                ErrorMessage.Text = $"Error al conectar a la base de datos: {ex.Message}";
            }
        }
        // --- FIN DE LA MODIFICACIÓN ---

        // --- MÉTODO NUEVO AÑADIDO ---
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Crea y muestra la nueva ventana de registro
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.ShowDialog(); // ShowDialog la abre de forma modal
        }

        // --- INICIO DE LA MODIFICACIÓN ---
        // Método nuevo para el botón "Salir de la Aplicación"
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // Cierra toda la aplicación
            Application.Current.Shutdown();
        }
        // --- FIN DE LA MODIFICACIÓN ---
    }
}