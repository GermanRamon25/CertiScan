using CertiScan.Services;
using System;
using System.Windows;

namespace CertiScan
{
    public partial class RegistrationWindow : Window
    {
        private readonly DatabaseService _databaseService;

        public RegistrationWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Text = ""; // Limpiar errores previos

            string fullName = FullNameTextBox.Text;
            string username = UsernameTextBox.Text;
            string password = PasswordInput.Password;
            string confirmPassword = ConfirmPasswordInput.Password;

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage.Text = "Todos los campos son obligatorios.";
                return;
            }

            if (password != confirmPassword)
            {
                ErrorMessage.Text = "Las contraseñas no coinciden.";
                return;
            }

            try
            {
                // Intentamos agregar el usuario a la base de datos
                bool success = _databaseService.AddUser(fullName, username, password);

                if (success)
                {
                    MessageBox.Show("¡Usuario registrado exitosamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close(); // Cierra la ventana de registro
                }
            }
            catch (Exception ex)
            {
                // Capturamos el error si el usuario ya existe u otro fallo
                ErrorMessage.Text = ex.Message;
            }
        }

        // Método agregado para el botón "Volver al Login"
        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}
