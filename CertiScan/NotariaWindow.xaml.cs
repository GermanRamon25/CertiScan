using System;
using System.Windows;
using CertiScan.Models;
using CertiScan.Services;

namespace CertiScan
{
    public partial class NotariaWindow : Window
    {
        public NotariaWindow()
        {
            InitializeComponent();
            CargarDatosExistentes();
        }

        private void CargarDatosExistentes()
        {
            try
            {
                // Obtenemos el ID de la notaría del usuario actual desde la sesión
                int notariaId = SessionService.UsuarioLogueado.NotariaId;

                // Llamamos al servicio para traer la info de SQL
                var info = DatabaseService.ObtenerDatosNotaria(notariaId);

                if (info != null)
                {
                    txtNombre.Text = info.NombreNotario;
                    txtNumero.Text = info.NumeroNotaria;
                    txtDireccion.Text = info.Direccion;
                    txtTelefono.Text = info.Telefono; // Campo independiente
                    txtEmail.Text = info.Email;       // Campo independiente
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos: " + ex.Message);
            }
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            var info = new NotariaInfo
            {
                Id = SessionService.UsuarioLogueado.NotariaId,
                NombreNotario = txtNombre.Text,
                NumeroNotaria = txtNumero.Text,
                Direccion = txtDireccion.Text,
                Telefono = txtTelefono.Text,
                Email = txtEmail.Text
            };

            // Intentamos actualizar en la base de datos
            bool exito = DatabaseService.ActualizarNotaria(info);

            if (exito)
            {
                MessageBox.Show("¡Datos actualizados correctamente para todos los usuarios!");
                this.Close();
            }
            else
            {
                MessageBox.Show("No se pudieron guardar los cambios.");
            }
        }
    }
}