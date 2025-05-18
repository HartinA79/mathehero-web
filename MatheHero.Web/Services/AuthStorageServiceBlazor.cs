using MatheHero.Shared.Shared.Interface;
using Blazored.LocalStorage;
using MatheHero.Shared.Helper;

namespace MatheHero.Web.Services
{
    public class AuthStorageServiceBlazor : IAuthStorageService
    {
        private readonly ILocalStorageService _localStorage;
        private const string Key = "auth_result";

        public AuthStorageServiceBlazor(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task SaveAuthAsync(AuthResult auth)
        {
            await _localStorage.SetItemAsync(Key, auth);
        }

        public async Task<AuthResult?> LoadAuthAsync()
        {
            return await _localStorage.GetItemAsync<AuthResult>(Key);
        }

        public async Task ClearAuthAsync()
        {
            await _localStorage.RemoveItemAsync(Key);
        }
    }
}
