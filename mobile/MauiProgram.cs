using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using mobile.Services;
using mobile.Services.Handlers;
using mobile.Pages.Auth;
using mobile.PageModels.Auth;
using mobile.Pages;
using mobile.PageModels;
using Microsoft.Extensions.DependencyInjection;

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
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            // Services
            builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
            builder.Services.AddSingleton<ModalErrorHandler>();

            // HttpClient avec authentification
            builder.Services.AddSingleton<AuthenticatedHttpClientHandler>();
            builder.Services.AddHttpClient<IApiService, ApiService>(client =>
            {
                var baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7252";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler<AuthenticatedHttpClientHandler>();

            // Pages et ViewModels d'authentification
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginPageModel>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<RegisterPageModel>();

            // Page principale
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainPageModel>();

            return builder.Build();
        }
    }
}
