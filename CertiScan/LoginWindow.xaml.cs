using CertiScan.Services;
using System;
using System.Windows;
using System.Windows.Controls;

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

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Text = "";
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            try
            {
                if (_databaseService.ValidateUser(username, password))
                {
                    // === CAMBIO CLAVE AQUÍ ===
                    // Obtenemos el objeto usuario completo (que ya trae el NotariaId corregido)
                    var user = _databaseService.GetUserByUsername(username);

                    if (user != null)
                    {
                        // Guardamos TODO el objeto en la sesión. 
                        // Esto soluciona el problema de que "no guarda" en la otra ventana.
                        SessionService.Login(user);
                    }
                    // =========================

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
                ErrorMessage.Text = $"Error al conectar a la base de datos: {ex.Message}";
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.ShowDialog();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}