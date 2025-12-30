using CertiScan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertiScan.Services
{
    public static class SessionService
    {
        // Agregamos esta propiedad para mantener toda la info del notario
        public static CertiScan.Models.Usuario UsuarioLogueado { get; private set; }

        public static int CurrentUserId => UsuarioLogueado?.Id ?? 0;
        public static string CurrentUserName => UsuarioLogueado?.NombreUsuario;

        public static void Login(CertiScan.Models.Usuario usuario)
        {
            UsuarioLogueado = usuario;
        }

        public static void Logout()
        {
            UsuarioLogueado = null;
        }

        internal static void Login(int id, string nombreUsuario)
        {
            // En lugar de lanzar la excepción, inicializamos el objeto
            UsuarioLogueado = new CertiScan.Models.Usuario
            {
                Id = id,
                NombreUsuario = nombreUsuario
            };
        }
    }
}
