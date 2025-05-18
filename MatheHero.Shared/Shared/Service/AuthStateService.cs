using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatheHero.Shared.Helper;
using MatheHero.Shared.Models;

namespace MatheHero.Shared.Shared.Service
{
    public class AuthStateService
    {
        private AuthResult _currentAuth;

        private UserModel _currentUser;

        public AuthResult CurrentAuth => _currentAuth;

        public UserModel CurrentUser => _currentUser;


        public bool IsAuthenticated => _currentAuth != null && !string.IsNullOrEmpty(_currentAuth.IdToken);

        public void SetUser (AuthResult auth, UserModel user)
        {
            _currentAuth = auth;
            _currentUser = user;
        }

        public AuthResult GetCurrentAuthStatus()
        {
            return _currentAuth;
        }

        public UserModel GetCurrentUserStatus()
        {
            return _currentUser;
        }

        public void Clear()
        {
            _currentUser = null;
            _currentAuth = null;
        }
    }
}
