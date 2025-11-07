using Microsoft.Extensions.Logging;
using mobile.Controls;

namespace mobile.Services
{
    /// <summary>
    /// Service de notification professionnel avec queue et animations
    /// Affiche les notifications en haut à droite (desktop uniquement)
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private NotificationManager? _notificationManager;
        private bool _isInitialized = false;

        public NotificationService (ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initialise le gestionnaire de notifications
        /// </summary>
        private async Task EnsureInitializedAsync ()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Vérifier si déjà initialisé
                if (_notificationManager != null && _notificationManager.Parent != null)
                {
                    return;
                }

                var currentPage = GetCurrentPage();
                if (currentPage == null)
                {
                    _logger.LogWarning("⚠️ Impossible d'initialiser le gestionnaire de notifications: Pas de page active");
                    _isInitialized = false;
                    _notificationManager = null;
                    return;
                }

                // Créer NotificationManager (notifications en haut à droite)
                _notificationManager = new NotificationManager();
                var overlayControl = _notificationManager;

                // L'ajouter à la page (par-dessus tout en overlay)
                if (currentPage.Content is Layout layout)
                {
                    if (layout is Grid grid)
                    {
                        // Grid: ajouter par-dessus tout (dernier enfant = au-dessus)
                        grid.Children.Add(overlayControl);
                    }
                    else if (layout is AbsoluteLayout absoluteLayout)
                    {
                        // AbsoluteLayout: ajouter par-dessus
                        absoluteLayout.Children.Add(overlayControl);
                    }
                    else
                    {
                        // Autres layouts: wrapper dans un Grid overlay
                        var wrapper = new Grid();
                        var parent = layout.Parent;

                        if (parent is ContentPage page)
                        {
                            page.Content = wrapper;
                        }

                        wrapper.Children.Add(layout);
                        wrapper.Children.Add(overlayControl);
                    }
                }
                else
                {
                    // Pas de layout: créer un Grid wrapper overlay
                    var wrapper = new Grid();
                    var oldContent = currentPage.Content;
                    currentPage.Content = wrapper;

                    if (oldContent != null)
                    {
                        wrapper.Children.Add(oldContent);
                    }

                    wrapper.Children.Add(overlayControl);
                }

                _isInitialized = true;
                _logger.LogInformation("✅ NotificationManager initialisé sur la page: {PageType}", currentPage?.GetType().Name);
            });
        }

        public async Task ShowSuccessAsync (string message, string? title = null)
        {
            await ShowNotificationAsync(title ?? "Succès", message, NotificationType.Success);
        }

        public async Task ShowInfoAsync (string message, string? title = null)
        {
            await ShowNotificationAsync(title ?? "Information", message, NotificationType.Info);
        }

        public async Task ShowWarningAsync (string message, string? title = null)
        {
            await ShowNotificationAsync(title ?? "Attention", message, NotificationType.Warning);
        }

        public async Task ShowErrorAsync (string message, string? title = null)
        {
            await ShowNotificationAsync(title ?? "Erreur", message, NotificationType.Error);
        }

        public async Task ShowForecastCreatedAsync (Models.WeatherForecast forecast)
        {
            var title = "Nouvelle Prévision";
            var message = $"{forecast.Date:dd/MM/yyyy} - {forecast.TemperatureC}°C - {forecast.Summary}";
            await ShowNotificationAsync(title, message, NotificationType.Success);
        }

        public async Task ShowForecastUpdatedAsync (Models.WeatherForecast forecast)
        {
            var title = "Prévision Modifiée";
            var message = $"{forecast.Date:dd/MM/yyyy} - {forecast.TemperatureC}°C - {forecast.Summary}";
            await ShowNotificationAsync(title, message, NotificationType.Info);
        }

        public async Task ShowForecastDeletedAsync (int forecastId)
        {
            var title = "Prévision Supprimée";
            var message = $"La prévision #{forecastId} a été supprimée";
            await ShowNotificationAsync(title, message, NotificationType.Warning);
        }

        private async Task ShowNotificationAsync (string title, string message, NotificationType type, int durationMs = 5000)
        {
            try
            {
                _logger.LogInformation("🔔 Tentative d'affichage notification: {Title} - {Message}", title, message);

                await EnsureInitializedAsync();

                if (_notificationManager != null)
                {
                    _logger.LogInformation("✅ NotificationManager disponible, affichage en cours...");
                    await _notificationManager.ShowNotificationAsync(title, message, type, durationMs);
                    _logger.LogInformation("📢 Notification affichée avec succès: {Title} - {Message}", title, message);
                }
                else
                {
                    _logger.LogWarning("❌ Gestionnaire de notifications non disponible (null)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'affichage de la notification: {Title} - {Message}", title, message);
            }
        }

        /// <summary>
        /// Réinitialise le gestionnaire de notifications (force la recréation)
        /// </summary>
        public void Reset ()
        {
            _logger.LogInformation("🔄 Réinitialisation du gestionnaire de notifications");
            _isInitialized = false;
            _notificationManager = null;
        }

        private ContentPage? GetCurrentPage ()
        {
            if (Application.Current?.MainPage is Shell shell)
            {
                return shell.CurrentPage as ContentPage;
            }
            else if (Application.Current?.MainPage is NavigationPage navPage)
            {
                return navPage.CurrentPage as ContentPage;
            }
            else if (Application.Current?.MainPage is ContentPage page)
            {
                return page;
            }

            return null;
        }
    }
}
