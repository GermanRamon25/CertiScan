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
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            // 1. Validar credenciales de usuario
            if (_databaseService.ValidateUser(username, password))
            {
                var user = _databaseService.GetUserByUsername(username);
                if (user != null)
                {
                    // 2. NUEVA VALIDACIÓN: Verificar estado de la anualidad y servicio activo
                    string mensajeSuscripcion;
                    if (!_databaseService.ValidarAccesoSistema(user.NotariaId, out mensajeSuscripcion))
                    {
                        // Si la validación falla (vencido o desactivado), mostramos el error y detenemos el login
                        MessageBox.Show(mensajeSuscripcion, "Aviso de Suscripción", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 3. Si la suscripción es válida, procedemos con el inicio de sesión
                    SessionService.Login(user); // Guarda la sesión

                    // 4. Verificar si la notaría tiene datos configurados
                    var notaria = _databaseService.ObtenerDatosNotaria(user.NotariaId);
                    if (notaria == null || string.IsNullOrEmpty(notaria.NumeroNotaria))
                    {
                        MessageBox.Show("Bienvenido. Por favor, configure los datos de su notaría antes de continuar.", "Configuración Inicial", MessageBoxButton.OK, MessageBoxImage.Information);
                        NotariaWindow notariaWin = new NotariaWindow();
                        notariaWin.Show();
                    }
                    else
                    {
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                    }

                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("Usuario o contraseña incorrectos.", "Error de Autenticación", MessageBoxButton.OK, MessageBoxImage.Error);
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