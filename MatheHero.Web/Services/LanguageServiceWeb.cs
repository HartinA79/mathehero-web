namespace MatheHero.Web.Services
{
    using System.Globalization;
    using System.Threading.Tasks;
    using MatheHero.Shared.Shared.Interface;
    using Microsoft.JSInterop;

    public partial class LanguageServiceWeb : ILanguageService
    {
        private const string LanguageKey = "app_language";
        private readonly IJSRuntime _js;

        public LanguageServiceWeb(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<string> LoadCultureAsync()
        {
            var culture = await _js.InvokeAsync<string>("localStorage.getItem", LanguageKey);
            return string.IsNullOrWhiteSpace(culture) ? "de-DE" : culture;
        }

        public void SaveCulture(string cultureCode)
        {
            _js.InvokeVoidAsync("localStorage.setItem", LanguageKey, cultureCode);
        }

        public void SetCulture(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            SaveCulture(cultureCode);
        }
    }
}
