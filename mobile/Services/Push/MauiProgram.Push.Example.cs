/*
 * EXEMPLE D'ENREGISTREMENT DES SERVICES PUSH DANS MauiProgram.cs
 * 
 * Ce fichier contient le code à ajouter dans MauiProgram.cs
 * quand vous serez prêt à activer les notifications push.
 * 
 * NE PAS UTILISER MAINTENANT - JUSTE POUR RÉFÉRENCE
 */

using mobile.Services.Push;

namespace mobile
{
    public static class MauiProgramPushExample
    {
        public static void ConfigurePushNotifications(IServiceCollection services)
        {
            // ============================================
            // ÉTAPE 1: Configuration
            // ============================================
            
            var pushConfig = new PushNotificationConfiguration
            {
                EnablePushNotifications = true,
                
                // Firebase (Android)
                FirebaseServerKey = "VOTRE_FIREBASE_SERVER_KEY",
                FirebaseSenderId = "VOTRE_FIREBASE_SENDER_ID",
                
                // APNS (iOS)
                ApnsKeyId = "VOTRE_APNS_KEY_ID",
                ApnsTeamId = "VOTRE_APNS_TEAM_ID",
                ApnsBundleId = "com.votreentreprise.weatherforecast",
                ApnsKeyPath = "path/to/AuthKey_XXXXXXXXXX.p8",
                ApnsApiUrl = "https://api.sandbox.push.apple.com", // ou production
            };

            services.AddSingleton(pushConfig);

            // ============================================
            // ÉTAPE 2: Enregistrer les services
            // ============================================

            // Service push selon la plateforme
#if ANDROID
            services.AddSingleton<IPushNotificationService, FirebasePushNotificationService>();
#elif IOS
            services.AddSingleton<IPushNotificationService, ApnsPushNotificationService>();
#else
            // Pas de push sur Windows/autres plateformes
            services.AddSingleton<IPushNotificationService, NullPushNotificationService>();
#endif

            // Service hybride (in-app + push)
            services.AddSingleton<HybridNotificationService>();

            // ============================================
            // ÉTAPE 3: Initialiser dans App.xaml.cs
            // ============================================
            
            /*
             * Dans App.xaml.cs, ajouter:
             * 
             * private readonly HybridNotificationService _hybridNotificationService;
             * 
             * public App(HybridNotificationService hybridNotificationService)
             * {
             *     InitializeComponent();
             *     _hybridNotificationService = hybridNotificationService;
             *     
             *     // Initialiser après la connexion
             *     // await _hybridNotificationService.InitializeAsync(userId);
             * }
             */
        }

        // ============================================
        // EXEMPLE D'UTILISATION
        // ============================================

        public static class UsageExamples
        {
            /*
             * Dans un PageModel ou Service:
             * 
             * private readonly HybridNotificationService _notificationService;
             * 
             * public MyService(HybridNotificationService notificationService)
             * {
             *     _notificationService = notificationService;
             * }
             * 
             * // Envoyer une notification
             * await _notificationService.SendNotificationAsync(
             *     userId: "user123",
             *     title: "Nouvelle Prévision",
             *     message: "Il va faire beau demain!",
             *     type: NotificationType.Success
             * );
             * 
             * // Envoyer une notification de forecast
             * await _notificationService.SendForecastCreatedNotificationAsync(
             *     userId: "user123",
             *     forecast: newForecast
             * );
             */
        }
    }

    // ============================================
    // SERVICE NULL POUR LES PLATEFORMES NON SUPPORTÉES
    // ============================================

    public class NullPushNotificationService : IPushNotificationService
    {
        public event EventHandler<PushNotificationReceivedEventArgs>? NotificationReceived;
        public event EventHandler<PushNotificationTappedEventArgs>? NotificationTapped;

        public Task InitializeAsync() => Task.CompletedTask;
        public Task<string?> GetDeviceTokenAsync() => Task.FromResult<string?>(null);
        public Task RegisterDeviceTokenAsync(string userId, string deviceToken) => Task.CompletedTask;
        public Task UnregisterDeviceTokenAsync(string userId) => Task.CompletedTask;
        public Task<bool> HasPermissionAsync() => Task.FromResult(false);
        public Task<bool> RequestPermissionAsync() => Task.FromResult(false);
        public Task SendNotificationAsync(string userId, string title, string message, Dictionary<string, string>? data = null) => Task.CompletedTask;
        public Task SendNotificationToMultipleAsync(List<string> userIds, string title, string message, Dictionary<string, string>? data = null) => Task.CompletedTask;
    }
}
