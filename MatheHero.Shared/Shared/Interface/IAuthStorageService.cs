
using MatheHero.Shared.Helper;

namespace MatheHero.Shared.Shared.Interface
{
    public interface IAuthStorageService
    {
        Task SaveAuthAsync(AuthResult auth);

        Task<AuthResult?> LoadAuthAsync();

        Task ClearAuthAsync();
    }
}
