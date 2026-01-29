using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertiScan.Models
{
    public class BusquedaHistorial
    {
        public string NombreUsuario { get; set; }
        public string TerminoBuscado { get; set; }
        public DateTime FechaCarga { get; set; }
        public bool ResultadoEncontrado { get; set; }

        public string ResultadoTexto => ResultadoEncontrado ? "Encontrado" : "No Encontrado";
    }
}
