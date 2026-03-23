using System;
using System.Windows;
using CertiScan.Models;
using CertiScan.Services;
using System.Text.RegularExpressions; // Necesario para la validación del @

namespace CertiScan
{
    public partial class NotariaWindow : Window
    {
        private readonly DatabaseService _dataService = new DatabaseService();

        public NotariaWindow()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            if (SessionService.UsuarioLogueado != null)
            {
                var info = _dataService.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);
                if (info != null)
                {
                    txtNombre.Text = info.NombreNotario == "Nueva Notaría" ? "" : info.NombreNotario;
                    txtNumero.Text = info.NumeroNotaria;
                    txtDireccion.Text = info.Direccion;
                    txtTelefono.Text = info.Telefono;
                    txtEmail.Text = info.Email;
                }
            }
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (SessionService.UsuarioLogueado == null) return;

            // 1. Validación de nombre obligatorio
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre del notario es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validación de formato de teléfono (10 dígitos)
            string telefono = txtTelefono.Text.Trim();
            if (telefono.Length != 10 || !long.TryParse(telefono, out _))
            {
                MessageBox.Show("El teléfono debe tener exactamente 10 dígitos numéricos.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. NUEVA VALIDACIÓN: Teléfono duplicado en la base de datos
            // Se verifica que el teléfono no pertenezca a otra notaría (Id distinto al actual)
            int miNotariaId = SessionService.UsuarioLogueado.NotariaId;
            if (_dataService.ExisteTelefonoEnOtraNotaria(telefono, miNotariaId))
            {
                MessageBox.Show("Este número de teléfono ya está registrado por otra notaría en el sistema.", "Teléfono Duplicado", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 4. Validación de correo electrónico con @ y formato válido
            string email = txtEmail.Text.Trim();
            if (!ValidarEmail(email))
            {
                MessageBox.Show("Por favor, ingrese un correo electrónico válido (debe contener '@' y un dominio).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var info = new NotariaInfo
            {
                Id = miNotariaId,
                NombreNotario = txtNombre.Text,
                NumeroNotaria = txtNumero.Text,
                Direccion = txtDireccion.Text,
                Telefono = telefono,
                Email = email
            };

            if (_dataService.ActualizarNotaria(info))
            {
                MessageBox.Show("Información sincronizada exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                MainWindow main = new MainWindow();
                main.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("No se encontró el registro de notaría para actualizar.");
            }
        } 

        // Función auxiliar para validar el formato del correo
        private bool ValidarEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            // Esta expresión regular verifica que tenga texto, luego un @, luego más texto y un punto.
            string patron = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, patron);
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}