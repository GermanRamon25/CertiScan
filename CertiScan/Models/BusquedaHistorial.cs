using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertiScan.Models
{
    public class BusquedaHistorial
    {
        // Se sugiere agregar el ID para tener una referencia única de la base de datos
        public int Id { get; set; }
        public string NombreUsuario { get; set; }
        public string TerminoBuscado { get; set; }
        public DateTime FechaCarga { get; set; }
        public bool ResultadoEncontrado { get; set; }

        // ESTA ES LA PROPIEDAD CLAVE PARA SEPARAR LOS MÓDULOS
        // Aquí guardarás "UIF" o "SAT" según corresponda
        public string Modulo { get; set; }

        public string ResultadoTexto => ResultadoEncontrado ? "Encontrado" : "No Encontrado";
    }
}