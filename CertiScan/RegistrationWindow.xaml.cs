using CertiScan.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

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

        // --- MÉTODOS PARA EL CAMPO "CONTRASEÑA" ---
        private void TogglePasswordCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            VisiblePasswordBox.Text = PasswordBox.Password;
            VisiblePasswordBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }

        private void TogglePasswordCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.Password = VisiblePasswordBox.Text;
            VisiblePasswordBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
        }

        private void VisiblePasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TogglePasswordCheckbox.IsChecked == true)
            {
                PasswordBox.Password = VisiblePasswordBox.Text;
            }
        }

        // --- MÉTODOS PARA EL CAMPO "CONFIRMAR CONTRASEÑA" ---
        private void ToggleConfirmPasswordCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            VisibleConfirmPasswordBox.Text = ConfirmPasswordBox.Password;
            VisibleConfirmPasswordBox.Visibility = Visibility.Visible;
            ConfirmPasswordBox.Visibility = Visibility.Collapsed;
        }

        private void ToggleConfirmPasswordCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            ConfirmPasswordBox.Password = VisibleConfirmPasswordBox.Text;
            VisibleConfirmPasswordBox.Visibility = Visibility.Collapsed;
            ConfirmPasswordBox.Visibility = Visibility.Visible;
        }

        private void VisibleConfirmPasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ToggleConfirmPasswordCheckbox.IsChecked == true)
            {
                ConfirmPasswordBox.Password = VisibleConfirmPasswordBox.Text;
            }
        }

        // --- BOTÓN REGISTRAR ---
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Text = "";

            string fullName = FullNameTextBox.Text;
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
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
                // 1. Intentamos agregar el usuario
                bool success = _databaseService.AddUser(fullName, username, password);

                if (success)
                {
                    MessageBox.Show("¡Usuario registrado exitosamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 2. Recuperamos el usuario de la BD para tener su ID y NotariaId
                    var user = _databaseService.GetUserByUsername(username);

                    if (user != null)
                    {
                        // 3. Iniciamos sesión automáticamente
                        SessionService.Login(user);

                        // 4. FLUJO DE VENTANAS:
                        // Cerramos el LoginWindow que quedó abierto en segundo plano
                        var loginWindow = Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault();
                        if (loginWindow != null) loginWindow.Close();

                        // Abrimos la ventana de Notaría
                        NotariaWindow notariaWin = new NotariaWindow();
                        notariaWin.Show();

                        // Cerramos esta ventana de registro
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = ex.Message;
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            // Si el usuario decide volver, nos aseguramos de no duplicar el Login si ya existe uno oculto
            var loginWindow = Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault();
            if (loginWindow == null)
            {
                loginWindow = new LoginWindow();
                loginWindow.Show();
            }
            else
            {
                loginWindow.Visibility = Visibility.Visible;
            }
            this.Close();
        }
    }
}