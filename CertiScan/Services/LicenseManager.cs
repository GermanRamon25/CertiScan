using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertiScan.Services
{
    public static class LicenseManager
    {
        private static readonly string LicenseFileName = "license.key";

        // --- ESTA ES TU CLAVE SECRETA ---
        // Puedes cambiarla por cualquier texto que quieras.
        // Esta es la clave que deberá contener el archivo "license.key".
        private static readonly string SecretKey = "Notarias-CertiScan-LicenciaValida-2025";

        public static bool ValidateLicense()
        {
            string licenseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseFileName);

            // 1. Verifica si el archivo "license.key" existe.
            if (!File.Exists(licenseFilePath))
            {
                // Si no existe, la licencia es inválida.
                return false;
            }

            try
            {
                // 2. Lee el contenido del archivo.
                string keyFromFile = File.ReadAllText(licenseFilePath).Trim();

                // 3. Compara el contenido del archivo con tu clave secreta.
                return keyFromFile == SecretKey;
            }
            catch
            {
                // Si hay cualquier error al leer el archivo, la licencia es inválida.
                return false;
            }
        }
    }
}
