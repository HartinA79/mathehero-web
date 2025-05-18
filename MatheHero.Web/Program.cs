using Blazored.LocalStorage;
using MatheHero.Shared.Services;
using MatheHero.Shared.Shared.Interface;
using MatheHero.Shared.Shared.Service;
using MatheHero.Web.Components;
using MatheHero.Web.Services;

namespace MatheHero
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<IPlatformService, WebPlaformService>();

            builder.Services.AddHttpClient<IAuthService, FirebaseAuthService>();
            builder.Services.AddHttpClient<IFirestoreService, FirestoreService>();

            builder.Services.AddSingleton<AvatarService>();
            builder.Services.AddSingleton<SchulService>();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddScoped<IAuthStorageService, AuthStorageServiceBlazor>();
            builder.Services.AddScoped<ILanguageService, LanguageServiceWeb>();
            builder.Services.AddSingleton<AuthStateService>();
            builder.Services.AddSingleton<ToastService>();

            builder.Services.AddLocalization(
                    options =>
                    {
                        options.ResourcesPath = "Resources";
                    }
                );

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Add device-specific services used by the MatheHero.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            var supportedCultures = new[] { "de-DE", "en-US", "it-IT" };
            var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            app.UseRequestLocalization(localizationOptions);

            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddAdditionalAssemblies(typeof(MatheHero.Shared._Imports).Assembly);

            app.Run();
        }
    }
}
