using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mobile.Services.Handlers;
using mobile.Services.Stores;
using mobile.Services.Theme;
using System.Reflection;

namespace mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp ()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
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
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", "FluentIcons");
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
            builder.Services.AddSingleton<IApiConfigurationService, ApiConfigurationService>();
            builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
            builder.Services.AddSingleton<IAuthenticationStateService, AuthenticationStateService>();
            builder.Services.AddSingleton<ISavedProfilesService, SavedProfilesService>();
            builder.Services.AddSingleton<INetworkMonitorService, NetworkMonitorService>();
            builder.Services.AddSingleton<IOfflineBannerManager, OfflineBannerManager>();
            builder.Services.AddSingleton<ISignalRService, SignalRService>();
            builder.Services.AddSingleton<ISessionValidationService, SessionValidationService>();
            builder.Services.AddSingleton<IStartupService, StartupService>();

            // Service de gestion des thèmes
            builder.Services.AddSingleton<IThemeService, ThemeService>();

            // Services de notification et conversations
            builder.Services.AddSingleton<INotificationStore, NotificationStore>();
            builder.Services.AddSingleton<IConversationStore, ConversationStore>();
            builder.Services.AddSingleton<INotificationService, NotificationService>();

            // Gestion des erreurs
            builder.Services.AddSingleton<IErrorHandler, ModalErrorHandler>();
            builder.Services.AddSingleton<GlobalExceptionHandler>();

            // HttpClient avec authentification
            builder.Services.AddTransient<AuthenticatedHttpClientHandler>();

            // Service de cache local (SQLite)
            builder.Services.AddSingleton<ICacheService, CacheService>();

            // Services API spécialisés (ISP - Interface Segregation Principle)
            builder.Services.AddHttpClient<IApiAuthService, ApiAuthService>((serviceProvider, client) =>
            {
                var apiConfig = serviceProvider.GetRequiredService<IApiConfigurationService>();
                client.BaseAddress = new Uri(apiConfig.GetBaseUrl());
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

            // Service API WeatherForecast avec cache
            builder.Services.AddHttpClient<IApiWeatherForecastService, ApiWeatherForecastServiceWithCache>((serviceProvider, client) =>
            {
                var apiConfig = serviceProvider.GetRequiredService<IApiConfigurationService>();
                client.BaseAddress = new Uri(apiConfig.GetBaseUrl());
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

            // Page de profil
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<ProfilePageModel>();

            // Page des conversations
            builder.Services.AddTransient<ConversationsPage>();
            builder.Services.AddTransient<ConversationDetailPage>();

            return builder.Build();
        }
    }
}
