using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mobile.Services.Handlers;
using Syncfusion.Maui.Toolkit.Hosting;
using System.Reflection;

namespace mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if IOS || MACCATALYST
    				handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Filled.ttf", "FluentIcons");
                    fonts.AddFont("SegMDL2.ttf", "SegoeMDL2");
                });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            // Charger appsettings.json depuis les ressources embarquées
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("mobile.Resources.Raw.appsettings.json");

            if (stream != null)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                builder.Configuration.AddConfiguration(config);
            }

            // Services
            builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
            builder.Services.AddSingleton<ISignalRService, SignalRService>();
            builder.Services.AddSingleton<ISessionValidationService, SessionValidationService>();
            builder.Services.AddSingleton<IStartupService, StartupService>();

            // Service de notification - Toasts personnalisés:
            builder.Services.AddSingleton<INotificationService, NotificationService>();

            // Gestion des erreurs
            builder.Services.AddSingleton<IErrorHandler, ModalErrorHandler>();
            builder.Services.AddSingleton<GlobalExceptionHandler>();

            // HttpClient avec authentification
            builder.Services.AddTransient<AuthenticatedHttpClientHandler>();
            builder.Services.AddHttpClient<IApiService, ApiService>(client =>
            {
                var baseUrl = "";
#if ANDROID
                // Utilise BaseUrlDevice pour téléphone réel, BaseUrlEmulator pour émulateur
                baseUrl = builder.Configuration["ApiSettings:BaseUrlDevice"]
                    ?? builder.Configuration["ApiSettings:BaseUrlEmulator"];
#elif IOS
                baseUrl = builder.Configuration["ApiSettings:BaseUrlDevice"] 
                    ?? builder.Configuration["ApiSettings:BaseUrlEmulator"];
#elif WINDOWS
                baseUrl = builder.Configuration["ApiSettings:BaseUrlWindows"];
#endif

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new InvalidOperationException("ApiSettings:BaseUrl* n'est pas configuré.");
                }

                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

            // Page de démarrage (Splash)
            builder.Services.AddTransient<SplashPage>();

            // Pages et ViewModels d'authentification
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginPageModel>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<RegisterPageModel>();

            // Page principale
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainPageModel>();

            // Page des prévisions météo
            builder.Services.AddTransient<ForecastsPage>();
            builder.Services.AddTransient<ForecastsPageModel>();

            return builder.Build();
        }
    }
}
