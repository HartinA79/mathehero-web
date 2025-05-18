using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatheHero.Shared.Helper;

namespace MatheHero.Shared.Shared.Interface
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(string email, string password, string username);

        Task<AuthResult> LoginAsync(string email, string password);

        Task<AuthResult> LoginWithGoogleAsync(string googleIdToken);

        Task<AuthResult> LoginAnonymouslyAsync();

        Task<string> GetIdTokenAsync();

        Task<AuthResult> GetAuthUserAsync();

        Task LogoutAsync();
    }
}
