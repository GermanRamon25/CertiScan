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
        private static readonly string LicenseFileName = "M.Bcl.System.Data.SNI.dll";

        // --- ESTA ES TU CLAVE SECRETA ---
        // Puedes cambiarla por cualquier texto que quieras.
        // Esta es la clave que deberá contener el archivo "license.key".
        private static readonly string SecretKey = "251B5A3C-4D2A-4F7E-9C8D-06A3B9E1F0C4";

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
