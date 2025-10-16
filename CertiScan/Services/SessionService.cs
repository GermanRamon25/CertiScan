using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertiScan.Services
{
    public static class SessionService
    {
        public static int CurrentUserId { get; private set; }
        public static string CurrentUserName { get; private set; }

        public static void Login(int userId, string userName)
        {
            CurrentUserId = userId;
            CurrentUserName = userName;
        }

        public static void Logout()
        {
            CurrentUserId = 0;
            CurrentUserName = null;
        }
    }
}
