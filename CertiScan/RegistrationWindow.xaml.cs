using CertiScan.Services;
using System;
using System.Windows;
using System.Windows.Controls;

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

        // Dentro de la clase public partial class RegistrationWindow : Window

        // --- MÉTODOS PARA EL CAMPO "CONTRASEÑA" (PasswordBox) ---

        private void TogglePasswordCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            // Muestra el TextBox, oculta el PasswordBox, y copia el texto
            VisiblePasswordBox.Text = PasswordBox.Password;
            VisiblePasswordBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }

        private void TogglePasswordCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Oculta el TextBox, muestra el PasswordBox, y copia el texto
            PasswordBox.Password = VisiblePasswordBox.Text;
            VisiblePasswordBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
        }

        private void VisiblePasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sincroniza en tiempo real mientras el TextBox es visible
            if (TogglePasswordCheckbox.IsChecked == true)
            {
                PasswordBox.Password = VisiblePasswordBox.Text;
            }
        }

        // --- MÉTODOS PARA EL CAMPO "CONFIRMAR CONTRASEÑA" (ConfirmPasswordBox) ---

        private void ToggleConfirmPasswordCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            // Muestra el TextBox, oculta el PasswordBox, y copia el texto
            VisibleConfirmPasswordBox.Text = ConfirmPasswordBox.Password;
            VisibleConfirmPasswordBox.Visibility = Visibility.Visible;
            ConfirmPasswordBox.Visibility = Visibility.Collapsed;
        }

        private void ToggleConfirmPasswordCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Oculta el TextBox, muestra el PasswordBox, y copia el texto
            ConfirmPasswordBox.Password = VisibleConfirmPasswordBox.Text;
            VisibleConfirmPasswordBox.Visibility = Visibility.Collapsed;
            ConfirmPasswordBox.Visibility = Visibility.Visible;
        }

        private void VisibleConfirmPasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sincroniza en tiempo real mientras el TextBox es visible
            if (ToggleConfirmPasswordCheckbox.IsChecked == true)
            {
                ConfirmPasswordBox.Password = VisibleConfirmPasswordBox.Text;
            }
        }
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Text = ""; // Limpiar errores previos

            string fullName = FullNameTextBox.Text;
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

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
