using Microsoft.Extensions.Logging;
using mobile.Controls;

namespace mobile.Services.Push
{
    /// <summary>
    /// Service hybride qui combine notifications in-app et push
    /// - App ouverte → Notification in-app (NotificationService)
    /// - App fermée/arrière-plan → Notification push (Firebase/APNS)
    /// </summary>
    public class HybridNotificationService
    {
        private readonly ILogger<HybridNotificationService> _logger;
        private readonly INotificationService _inAppNotificationService;
        private readonly IPushNotificationService _pushNotificationService;
        private bool _isAppInForeground = true;

        public HybridNotificationService(
            ILogger<HybridNotificationService> logger,
            INotificationService inAppNotificationService,
            IPushNotificationService pushNotificationService)
        {
            _logger = logger;
            _inAppNotificationService = inAppNotificationService;
            _pushNotificationService = pushNotificationService;

            // S'abonner aux événements du cycle de vie de l'app
            SubscribeToAppLifecycleEvents();

            // S'abonner aux notifications push reçues
            _pushNotificationService.NotificationReceived += OnPushNotificationReceived;
            _pushNotificationService.NotificationTapped += OnPushNotificationTapped;
        }

        /// <summary>
        /// Initialise le service hybride
        /// </summary>
        public async Task InitializeAsync(string userId)
        {
            try
            {
                _logger.LogInformation("🔄 Initialisation du service hybride de notifications...");

                // Initialiser les notifications push
                await _pushNotificationService.InitializeAsync();

                // Obtenir et enregistrer le device token
                var deviceToken = await _pushNotificationService.GetDeviceTokenAsync();
                if (!string.IsNullOrEmpty(deviceToken))
                {
                    await _pushNotificationService.RegisterDeviceTokenAsync(userId, deviceToken);
                }

                _logger.LogInformation("✅ Service hybride initialisé");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'initialisation du service hybride");
            }
        }

        /// <summary>
        /// Envoie une notification (in-app ou push selon l'état de l'app)
        /// </summary>
        public async Task SendNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            Dictionary<string, string>? data = null)
        {
            try
            {
                // Si l'app est au premier plan → Notification in-app
                if (_isAppInForeground)
                {
                    _logger.LogInformation("📱 App au premier plan → Notification in-app");

                    switch (type)
                    {
                        case NotificationType.Success:
                            await _inAppNotificationService.ShowSuccessAsync(message, title);
                            break;
                        case NotificationType.Error:
                            await _inAppNotificationService.ShowErrorAsync(message, title);
                            break;
                        case NotificationType.Warning:
                            await _inAppNotificationService.ShowWarningAsync(message, title);
                            break;
                        case NotificationType.Info:
                        default:
                            await _inAppNotificationService.ShowInfoAsync(message, title);
                            break;
                    }
                }
                // Si l'app est en arrière-plan ou fermée → Notification push
                else
                {
                    _logger.LogInformation("📤 App en arrière-plan → Notification push");
                    await _pushNotificationService.SendNotificationAsync(userId, title, message, data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'envoi de la notification");
            }
        }

        /// <summary>
        /// Envoie une notification de forecast créé
        /// </summary>
        public async Task SendForecastCreatedNotificationAsync(
            string userId,
            Models.WeatherForecast forecast)
        {
            var title = "Nouvelle Prévision";
            var message = $"{forecast.Date:dd/MM/yyyy} - {forecast.TemperatureC}°C - {forecast.Summary}";
            var data = new Dictionary<string, string>
            {
                { "type", "forecast_created" },
                { "forecastId", forecast.Id.ToString() },
                { "date", forecast.Date.ToString("yyyy-MM-dd") }
            };

            if (_isAppInForeground)
            {
                await _inAppNotificationService.ShowForecastCreatedAsync(forecast);
            }
            else
            {
                await _pushNotificationService.SendNotificationAsync(userId, title, message, data);
            }
        }

        /// <summary>
        /// Envoie une notification de forecast modifié
        /// </summary>
        public async Task SendForecastUpdatedNotificationAsync(
            string userId,
            Models.WeatherForecast forecast)
        {
            var title = "Prévision Modifiée";
            var message = $"{forecast.Date:dd/MM/yyyy} - {forecast.TemperatureC}°C - {forecast.Summary}";
            var data = new Dictionary<string, string>
            {
                { "type", "forecast_updated" },
                { "forecastId", forecast.Id.ToString() }
            };

            if (_isAppInForeground)
            {
                await _inAppNotificationService.ShowForecastUpdatedAsync(forecast);
            }
            else
            {
                await _pushNotificationService.SendNotificationAsync(userId, title, message, data);
            }
        }

        /// <summary>
        /// Envoie une notification de forecast supprimé
        /// </summary>
        public async Task SendForecastDeletedNotificationAsync(
            string userId,
            int forecastId)
        {
            var title = "Prévision Supprimée";
            var message = $"La prévision #{forecastId} a été supprimée";
            var data = new Dictionary<string, string>
            {
                { "type", "forecast_deleted" },
                { "forecastId", forecastId.ToString() }
            };

            if (_isAppInForeground)
            {
                await _inAppNotificationService.ShowForecastDeletedAsync(forecastId);
            }
            else
            {
                await _pushNotificationService.SendNotificationAsync(userId, title, message, data);
            }
        }

        /// <summary>
        /// S'abonne aux événements du cycle de vie de l'app
        /// </summary>
        private void SubscribeToAppLifecycleEvents()
        {
            // Détecter quand l'app passe au premier plan ou en arrière-plan
            Application.Current!.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Application.Current.UserAppTheme))
                {
                    // L'app est au premier plan
                    _isAppInForeground = true;
                    _logger.LogInformation("📱 App au premier plan");
                }
            };

            // Alternative: Utiliser les événements de Window
            // Window.Activated → App au premier plan
            // Window.Deactivated → App en arrière-plan
        }

        /// <summary>
        /// Appelé quand une notification push est reçue (app ouverte)
        /// </summary>
        private async void OnPushNotificationReceived(object? sender, PushNotificationReceivedEventArgs e)
        {
            _logger.LogInformation("📬 Notification push reçue: {Title}", e.Title);

            // Si l'app est ouverte, afficher une notification in-app
            if (_isAppInForeground)
            {
                await _inAppNotificationService.ShowInfoAsync(e.Message, e.Title);
            }
        }

        /// <summary>
        /// Appelé quand l'utilisateur clique sur une notification push
        /// </summary>
        private void OnPushNotificationTapped(object? sender, PushNotificationTappedEventArgs e)
        {
            _logger.LogInformation("👆 Notification push cliquée: {Title}", e.Title);

            // Naviguer vers la page appropriée selon le type
            if (e.Data.TryGetValue("type", out var type))
            {
                switch (type)
                {
                    case "forecast_created":
                    case "forecast_updated":
                    case "forecast_deleted":
                        // Naviguer vers la page des prévisions
                        // Shell.Current.GoToAsync("//forecasts");
                        break;
                }
            }
        }

        /// <summary>
        /// Nettoie les ressources lors de la déconnexion
        /// </summary>
        public async Task CleanupAsync(string userId)
        {
            try
            {
                _logger.LogInformation("🧹 Nettoyage du service hybride...");
                await _pushNotificationService.UnregisterDeviceTokenAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du nettoyage");
            }
        }
    }
}
