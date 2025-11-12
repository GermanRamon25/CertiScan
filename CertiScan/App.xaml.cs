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

namespace CertiScan
{
    public partial class App : Application
    {

        private void Application_Startup(object sender, StartupEventArgs e)
        {

            // Manejador para excepciones de UI (Hilos principales)
            Application.Current.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Manejador para excepciones de no-UI (Otros hilos)
            AppDomain.CurrentDomain.UnhandledException += App_CurrentDomain_UnhandledException;
          


            // Validacion de la licencia al iniciar la aplicación
            if (!LicenseManager.ValidateLicense())
            {
                // Si la licencia no es válida, mostramos un error y cerra la app.
                MessageBox.Show("La licencia de la aplicación no es válida para este equipo.", "Error de Licencia", MessageBoxButton.OK, MessageBoxImage.Error);

             
                this.Shutdown();
            }
            // Si la licencia es válida, el programa continuará y abrirá la LoginWindow normalmente.
        }

   
        // método se dispara cuando hay un error en el hilo principal (UI)
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Se marca como "manejado" para que ni se cierre de golpe la aplicacion 
            e.Handled = true;
            LogException(e.Exception);
        }
       

      
        // Este método se dispara cuando hay un error en cualquier otro hilo
        private void App_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception);
        }
       

    
        // Método centralizado para escribir el log y notificar al usuario
        private void LogException(Exception ex)
        {
            if (ex == null) return;

            // Define dónde se guardará el log (en la misma carpeta del .exe)
            string errorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CertiScan_ErrorLog.txt");
            string errorMessage = $"================ {DateTime.Now} ================\n" +
                                  $"Error: {ex.Message}\n" +
                                  $"Tipo de Error: {ex.GetType().Name}\n" +
                                  $"Stack Trace:\n{ex.StackTrace}\n\n";

            // Intenta escribir el error en el archivo de log
            try
            {
                File.AppendAllText(errorLogPath, errorMessage);
                MessageBox.Show($"Ocurrió un error inesperado. El programa debe cerrarse.\n\n" +
                                $"Se ha guardado un registro del error en:\n{errorLogPath}",
                                "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception)
            {
                // Fallback por si no puede escribir el log (ej. permisos)
                MessageBox.Show($"Error crítico: {ex.Message}", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Cierra la aplicación
            Application.Current.Shutdown();
        }
        
    }
}