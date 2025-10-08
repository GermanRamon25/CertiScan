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
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            // Validaciones básicas
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
                // Capturamos el error si el usuario ya existe
                ErrorMessage.Text = ex.Message;
            }
        }
    }
}