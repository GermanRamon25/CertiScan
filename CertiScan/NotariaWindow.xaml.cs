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

            var info = new NotariaInfo
            {
                Id = SessionService.UsuarioLogueado.NotariaId,
                NombreNotario = txtNombre.Text,
                NumeroNotaria = txtNumero.Text,
                Direccion = txtDireccion.Text,
                Telefono = txtTelefono.Text,
                Email = txtEmail.Text
            };

            // El método 'ActualizarNotaria' ya lo tienes en tu DataService
            if (_dataService.ActualizarNotaria(info))
            {
                MessageBox.Show("Información actualizada para todos los usuarios de la notaría.", "Éxito");
                this.Close();
            }
            else
            {
                MessageBox.Show("Error al guardar en la base de datos.", "Error");
            }
        }
    }
}