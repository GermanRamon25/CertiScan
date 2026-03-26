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
        public string RutaArchivo { get; set; }

        // Mantenemos 'Contenido' por compatibilidad con métodos antiguos si existen
        public string Contenido { get; set; }

        // AGREGAMOS 'ContenidoTexto' para que coincida con la lógica de búsqueda 
        // y con el nombre de la columna en tu BASE DE DATOS 2.0.sql
        public string ContenidoTexto
        {
            get => Contenido;
            set => Contenido = value;
        }
    }
}