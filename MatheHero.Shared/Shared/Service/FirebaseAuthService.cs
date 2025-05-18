using System.Net.Http.Json;
using System.Text.Json;
using MatheHero.Shared.Helper;
using MatheHero.Shared.Models;
using MatheHero.Shared.Shared.Interface;

namespace MatheHero.Shared.Shared.Service
{
    public class FirebaseAuthService : IAuthService
    {
        private readonly HttpClient _httpClient;

        private readonly IAuthStorageService _authStorage;

        private readonly AuthStateService _authState;

        private readonly ToastService _toastService;

        private readonly string _apiKey = "AIzaSyBp-ezAW6-oWm3MQgjAyqOj5jKjJyb_xYo";

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private AuthResult _currentAuthResult;

        public DateTimeOffset _tokenExpiry { get; set; } = DateTimeOffset.MinValue;

        public FirebaseAuthService(HttpClient httpClient, IAuthStorageService authStorage, ToastService toastService)
        {
            _httpClient = httpClient;
            _authStorage = authStorage;
            this._toastService = toastService;
        }

        public async Task<AuthResult> RegisterAsync(string email, string password, string username)
        {
            var payload = new { email = email, password = password, returnSecureToken = true };

            var response = await _httpClient.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}",
                    payload
                );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _toastService.ShowToast("", ExtractFirebaseError(json), ToastType.Error);
                return null;
            }

            //response.EnsureSuccessStatusCode();

            //using var stream = await response.Content.ReadAsStreamAsync();
            var result = JsonSerializer.Deserialize<AuthResult>(json, _jsonOptions);

            return result;
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}",
                payload
            );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _toastService.ShowToast("", ExtractFirebaseError(json), ToastType.Error);
                return null;
            }

            var result = JsonSerializer.Deserialize<AuthResult>(json, _jsonOptions);

            if (result == null)
            {
                _toastService.ShowToast("", MessageType.RESPONSE_ERROR, ToastType.Error);
                return null;
            }

            _currentAuthResult = result;
            _tokenExpiry = DateTimeOffset.UtcNow
                            .AddSeconds(int.Parse(result.ExpiresIn))
                            .Subtract(TimeSpan.FromMinutes(1)); // Sicherheitspuffer

            await _authStorage.SaveAuthAsync(result);

            return result;
        }

        public async Task<AuthResult> LoginWithGoogleAsync(string googleIdToken)
        {
            var payload = new
            {
                postBody = $"id_token={googleIdToken}&providerId=google.com",
                requestUri = "http://localhost",
                returnSecureToken = true,
                returnIdpCredential = true,
            };

            var response = await _httpClient.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={_apiKey}",
                    payload
                );

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<AuthResult>(stream);

            if (result == null)
                throw new Exception("Login Google failed: Keine gültige Antwort erhalten");

            _currentAuthResult = result ?? new AuthResult();
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(int.Parse(_currentAuthResult.ExpiresIn)).Subtract(TimeSpan.FromMinutes(1));

            return result;
        }

        public async Task<AuthResult> LoginAnonymouslyAsync()
        {
            var payload = new { returnSecureToken = true };

            var response = await _httpClient.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}",
                    payload
                );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _toastService.ShowToast("", ExtractFirebaseError(json), ToastType.Error);
                return null;
            }

            var result = JsonSerializer.Deserialize<AuthResult>(json, _jsonOptions);

            if (result == null)
            {
                _toastService.ShowToast("", MessageType.RESPONSE_ERROR, ToastType.Error);
                return null;
            }

            _currentAuthResult = result;
            _currentAuthResult.DisplayName = "John Doe";
            _tokenExpiry = DateTimeOffset.UtcNow
                            .AddSeconds(int.Parse(result.ExpiresIn))
                            .Subtract(TimeSpan.FromMinutes(1)); // Sicherheitspuffer

            await _authStorage.SaveAuthAsync(result);

            return result;
        }

        public async Task<string> GetIdTokenAsync()
        {
            if (_currentAuthResult == null)
                return string.Empty;

            if (TokenIsExpired(_currentAuthResult))
                try
                {
                    var refreshed =  await RefreshIdTokenAsync(_currentAuthResult.RefreshToken);

                    _currentAuthResult = refreshed;

                    _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(int.Parse(refreshed.ExpiresIn)).Subtract(TimeSpan.FromMinutes(1));
                }
                catch
                {
                    _currentAuthResult = null;
                    return string.Empty;
                }

            return _currentAuthResult.IdToken;
        }

        private async Task<AuthResult> RefreshIdTokenAsync(string refreshToken)
        {
            var requestContent = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };

            var response = await _httpClient.PostAsync(
                    $"https://securetoken.googleapis.com/v1/token?key={_apiKey}",
                    new FormUrlEncodedContent(requestContent)
                );

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            var refreshResult = await JsonSerializer.DeserializeAsync<RefreshTokenResult>(stream);

            return new AuthResult
            {
                IdToken = refreshResult.IdToken,
                RefreshToken = refreshResult.RefreshToken,
                ExpiresIn = refreshResult.ExpiresIn,
                LocalId = refreshResult.ProjectId,
            };
        }

        private bool TokenIsExpired(AuthResult authResult)
        {
            return DateTimeOffset.UtcNow >= _tokenExpiry;
        }

        public async Task<AuthResult> GetAuthUserAsync()
        {
            return _currentAuthResult;
        }

        public Task LogoutAsync()
        {
            _currentAuthResult = null;
            _tokenExpiry = DateTimeOffset.MinValue;

            // Optional: Falls du Tokens persistent gespeichert hast (z. B. in SecureStorage), lösche sie hier:
#if ANDROID || IOS || WINDOWS
    SecureStorage.Remove("IdToken");
    SecureStorage.Remove("RefreshToken");
    SecureStorage.Remove("TokenExpiry");
#endif
            _authStorage.ClearAuthAsync();
   
            return Task.CompletedTask;
        }

        private MessageType ExtractFirebaseError(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var code = doc.RootElement.GetProperty("error").GetProperty("message").GetString();

                return code switch
                {
                    "EMAIL_EXISTS" => MessageType.EMAIL_EXISTS,
                    "INVALID_LOGIN_CREDENTIALS" => MessageType.INVALID_LOGIN_CREDENTIALS,
                    "INVALID_EMAIL" => MessageType.INVALID_EMAIL,
                    "WEAK_PASSWORD" => MessageType.WEAK_PASSWORD,
                    "EMAIL_NOT_FOUND" => MessageType.EMAIL_NOT_FOUND,
                    "INVALID_PASSWORD" => MessageType.INVALID_PASSWORD,
                    "USER_DISABLED" => MessageType.USER_DISABLED,
                    "TOO_MANY_ATTEMPTS_TRY_LATER" => MessageType.TOO_MANY_ATTEMPTS_TRY_LATER,
                    "OPERATION_NOT_ALLOWED" => MessageType.OPERATION_NOT_ALLOWED,
                    _ => MessageType.DEFAULT
                };
            }
            catch
            {
                return MessageType.UNKNOWN_ERROR;
            }
        }
    }
}
