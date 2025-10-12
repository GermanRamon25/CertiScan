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

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordInput.Password;

            if (_databaseService.ValidateUser(username, password))
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                ErrorMessage.Text = "Usuario o contraseña incorrectos.";
            }
        }

        // --- MÉTODO NUEVO AÑADIDO ---
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Crea y muestra la nueva ventana de registro
            RegistrationWindow registrationWindow = new RegistrationWindow();
            registrationWindow.ShowDialog(); // ShowDialog la abre de forma modal
        }
    }
}