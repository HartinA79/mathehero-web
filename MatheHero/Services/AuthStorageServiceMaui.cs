using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MatheHero.Shared.Helper;
using MatheHero.Shared.Shared.Interface;

namespace MatheHero.Services
{
    public class AuthStorageServiceMaui : IAuthStorageService
    {
        private const string Key = "auth_result";

        public async Task SaveAuthAsync(AuthResult auth)
        {
            var json = JsonSerializer.Serialize(auth);
            await SecureStorage.SetAsync(Key, json);
        }

        public async Task<AuthResult?> LoadAuthAsync()
        {
            var json = await SecureStorage.GetAsync(Key);
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<AuthResult>(json);
        }

        public Task ClearAuthAsync()
        {
            SecureStorage.Remove(Key);
            return Task.CompletedTask;
        }
    }
}
