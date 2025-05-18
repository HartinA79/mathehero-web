using MatheHero.Services;
using MatheHero.Shared.Services;
using MatheHero.Shared.Shared.Interface;
using MatheHero.Shared.Shared.Service;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace MatheHero
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("Humor.ttf", "Humor");
                });

            builder.Services.AddSingleton<IPlatformService, AppPlatformService>();
            builder.Services.AddHttpClient<IAuthService, FirebaseAuthService>();
            builder.Services.AddHttpClient<IFirestoreService, FirestoreService>();
            builder.Services.AddSingleton<ILanguageService, LanguageServiceMaui>();
            builder.Services.AddSingleton<AvatarService>();
            builder.Services.AddSingleton<SchulService>();
            builder.Services.AddSingleton<IAuthStorageService, AuthStorageServiceMaui>();
            builder.Services.AddSingleton<AuthStateService>();
            builder.Services.AddSingleton<ToastService>();

            builder.Services.AddLocalization(option =>
            {
                option.ResourcesPath = "Resources";
            });

            builder.Services.AddSingleton<IStringLocalizerFactory, ResourceManagerStringLocalizerFactory>();
            builder.Services.AddSingleton(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

            // Add device-specific services used by the MatheHero.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
