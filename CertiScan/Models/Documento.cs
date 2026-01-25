 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertiScan.Models
{
    public class Documento
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; }
        public DateTime FechaCarga { get; set; }

        // Esta propiedad es la que usará el visor para mostrar el PDF
        public string RutaArchivo { get; set; }
    }
}