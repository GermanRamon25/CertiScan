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
        // Propiedad para guardar el perfil completo del usuario logueado
        public static CertiScan.Models.Usuario UsuarioLogueado { get; set; }

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
    }
}
