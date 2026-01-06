using System;
using System.Windows;
using CertiScan.Models;
using CertiScan.Services;

namespace CertiScan
{
    public partial class NotariaWindow : Window
    {
        // Usamos DataService que es el nombre real de tu clase
        private readonly DatabaseService _dataService = new DatabaseService();

        public NotariaWindow()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            // En tu SessionService, el usuario logueado está en 'UsuarioLogueado'
            if (SessionService.UsuarioLogueado != null)
            {
                // El método correcto en tu DataService es 'ObtenerDatosNotaria'
                var info = _dataService.ObtenerDatosNotaria(SessionService.UsuarioLogueado.NotariaId);
                if (info != null)
                {
                    txtNombre.Text = info.NombreNotario;
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

            // Validación de teléfono de 10 dígitos
            string telefono = txtTelefono.Text.Trim();
            if (telefono.Length != 10 || !long.TryParse(telefono, out _))
            {
                MessageBox.Show("El teléfono debe tener exactamente 10 dígitos.");
                return;
            }

            var info = new NotariaInfo
            {
                Id = SessionService.UsuarioLogueado.NotariaId,
                NombreNotario = txtNombre.Text,
                NumeroNotaria = txtNumero.Text,
                Direccion = txtDireccion.Text,
                Telefono = telefono,
                Email = txtEmail.Text
            };

            if (_dataService.ActualizarNotaria(info))
            {
                MessageBox.Show("Información sincronizada exitosamente.", "Éxito");
                this.Close();
            }
            else
            {
                MessageBox.Show("No se encontró el registro de notaría para actualizar.");
            }
        }
    }
}