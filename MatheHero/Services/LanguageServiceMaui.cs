using System.Globalization;
using MatheHero.Shared.Shared.Interface;
using Microsoft.Maui.Storage;

namespace MatheHero.Services
{
    public partial class LanguageServiceMaui : ILanguageService
    {
        private const string LanguageKey = "app_language";

        public Task<string> LoadCultureAsync()
        {
            var culture = Preferences.Get(LanguageKey, "de-DE");
            return Task.FromResult(culture);
        }

        public void SaveCulture(string culture)
        {
            Preferences.Set(LanguageKey, culture);
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