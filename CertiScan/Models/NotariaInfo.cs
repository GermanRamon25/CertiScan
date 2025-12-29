using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertiScan.Models
{
    public class NotariaInfo
    {
        public int Id { get; set; }
        public string NombreNotario { get; set; }
        public string NumeroNotaria { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
    }
}
