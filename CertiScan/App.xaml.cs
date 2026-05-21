using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CertiScan.Services;
using System.IO;
using System.Windows.Threading;
using QuestPDF.Infrastructure; // <- Espacio de nombres necesario para la licencia

namespace CertiScan
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // ---------------------------------------------------------
            // REGISTRO GLOBAL DE LICENCIA DE QUESTPDF (DESTRABA LA LIBRERÍA)
            // ---------------------------------------------------------
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
            }
            catch (Exception ex)
            {
                // Si ocurre un fallo inesperado con el componente gráfico, lo atrapa de forma segura
                System.Diagnostics.Debug.WriteLine($"Error al registrar licencia de QuestPDF: {ex.Message}");
            }

            // Manejador para excepciones de UI (Hilos principales)
            Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Manejador para excepciones de no-UI (Otros hilos)
            AppDomain.CurrentDomain.UnhandledException += App_CurrentDomain_UnhandledException;

            // ---------------------------------------------------------
            // ELIMINAR O COMENTAR ESTE BLOQUE COMPLETO
            // ---------------------------------------------------------
            /* if (!LicenseManager.ValidateLicense())
            {
                // Si la licencia no es válida, mostramos un error y cerra la app.
                MessageBox.Show("La licencia de la aplicación no es válida para este equipo.", "Error de Licencia", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Shutdown();
            }
            */
            // ---------------------------------------------------------

            // Al quitar lo anterior, el programa abrirá la ventana de inicio (definida en tu App.xaml) automáticamente.
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogException(e.Exception);
        }

        private void App_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception);
        }

        private void LogException(Exception ex)
        {
            if (ex == null) return;

            // ... (Tu código de log existente permanece aquí para registrar el error) ...

            // ----------------------------------------------------------------------------------
            // MODIFICACIÓN CRÍTICA: Se eliminó la línea "Application.Current.Shutdown();" 
            // para evitar que falte un dato o salte una advertencia y te apague el sistema.
            // ----------------------------------------------------------------------------------
        }
    }
}