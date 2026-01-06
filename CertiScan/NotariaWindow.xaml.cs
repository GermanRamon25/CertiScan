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
            try
            {
                // 1. Verificación de Sesión
                if (SessionService.UsuarioLogueado == null)
                {
                    MessageBox.Show("Error: No hay una sesión de usuario activa.");
                    return;
                }

                // 2. Validación de Teléfono (10 dígitos)
                string telefono = txtTelefono.Text.Trim();
                if (telefono.Length != 10 || !long.TryParse(telefono, out _))
                {
                    MessageBox.Show("El teléfono debe contener exactamente 10 dígitos numéricos.");
                    return;
                }

                // 3. Crear objeto con los datos de los TextBox del XAML
                var info = new NotariaInfo
                {
                    Id = SessionService.UsuarioLogueado.NotariaId,
                    NombreNotario = txtNombre.Text,
                    NumeroNotaria = txtNumero.Text,
                    Direccion = txtDireccion.Text,
                    Telefono = telefono,
                    Email = txtEmail.Text
                };

                // 4. Intento de actualización en DB
                if (_dataService.ActualizarNotaria(info))
                {
                    MessageBox.Show("Datos sincronizados con éxito.", "CertiScan");
                    this.Close();
                }
                else
                {
                    MessageBox.Show("No se pudo actualizar. Verifique que el ID de la notaría sea correcto.");
                }
            }
            catch (Exception ex)
            {
                // Esto te dirá el error real (ej. problema de conexión o de SQL)
                MessageBox.Show("Error técnico: " + ex.Message);
            }
        }
    }
}