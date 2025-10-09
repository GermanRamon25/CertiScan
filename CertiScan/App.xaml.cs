using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CertiScan.Services;

namespace CertiScan
{
    public partial class App : Application
    {
        // --- CÓDIGO AÑADIDO ---
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Validamos la licencia al iniciar la aplicación
            if (!LicenseManager.ValidateLicense())
            {
                // Si la licencia no es válida, mostramos un error y cerramos la app.
                MessageBox.Show("La licencia de la aplicación no es válida para este equipo.", "Error de Licencia", MessageBoxButton.OK, MessageBoxImage.Error);

                // Cierra la aplicación
                this.Shutdown();
            }
            // Si la licencia es válida, el programa continuará y abrirá la LoginWindow normalmente.
        }
    }
}