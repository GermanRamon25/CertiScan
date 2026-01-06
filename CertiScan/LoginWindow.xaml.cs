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

            // Dentro de LoginButton_Click en LoginWindow.xaml.cs
            if (_databaseService.ValidateUser(username, password))
            {
                var user = _databaseService.GetUserByUsername(username);
                if (user != null)
                {
                    SessionService.Login(user); // Guarda la sesión

                    // Verificar si la notaría está configurada
                    var notaria = _databaseService.ObtenerDatosNotaria(user.NotariaId);
                    if (notaria == null || string.IsNullOrEmpty(notaria.NumeroNotaria))
                    {
                        MessageBox.Show("Bienvenido. Por favor, configure los datos de su notaría antes de continuar.");
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