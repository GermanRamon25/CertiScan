using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CertiScan.Services
{
    public static class LicenseManager
    {
        private static readonly string LicenseFileName = "license.key";

        // --- ESTA ES TU CLAVE SECRETA ---
        // Puedes cambiarla por cualquier texto que quieras.
        // Esta es la clave que deberá contener el archivo "license.key".
        private static readonly string SecretKey = "Notaria215-CertiScan-LicenciaValida-2025";

        public static bool ValidateLicense()
        {
            // Ahora el programa sabe qué es "Path"
            string licenseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseFileName);

            // Ahora el programa sabe qué es "File"
            if (!File.Exists(licenseFilePath))
            {
                return false;
            }

            try
            {
                string keyFromFile = File.ReadAllText(licenseFilePath).Trim();
                return keyFromFile == SecretKey;
            }
            catch
            {
                return false;
            }
        }
    }
}
