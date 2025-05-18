namespace MatheHero.Shared.Shared.Interface
{
    public interface ILanguageService
    {
        void SetCulture(string cultureCode);

        void SaveCulture(string cultureCode);

        Task<string> LoadCultureAsync();
    }
}
