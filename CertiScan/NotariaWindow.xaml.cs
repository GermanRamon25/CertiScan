using System;
using System.Windows;
using CertiScan.Models;
using CertiScan.Services;

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
                    // CORRECCIÓN: Si el nombre es el de por defecto "Nueva Notaría", 
                    // lo dejamos vacío para que el usuario escriba su nombre real.
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

            // Validación de campos obligatorios mínimos
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre del notario es obligatorio.");
                return;
            }

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
                MessageBox.Show("Información sincronizada exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // CORRECCIÓN DE FLUJO: 
                // Abrimos el programa principal automáticamente después de configurar.
                MainWindow main = new MainWindow();
                main.Show();

                // Cerramos esta ventana de configuración.
                this.Close();
            }
            else
            {
                MessageBox.Show("No se encontró el registro de notaría para actualizar.");
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            // Si el usuario cancela, simplemente cerramos la ventana.
            this.Close();
        }
    }
}