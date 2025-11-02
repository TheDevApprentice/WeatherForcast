using Microsoft.Extensions.Logging;
using mobile.Controls;

namespace mobile.Services
{
    /// <summary>
    /// Service de notification professionnel avec queue et animations
    /// Affiche les notifications en haut à droite comme sur le web
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private NotificationManager? _notificationManager;
        private bool _isInitialized = false;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initialise le gestionnaire de notifications
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized && _notificationManager != null)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var currentPage = GetCurrentPage();
                if (currentPage == null)
                {
                    _logger.LogWarning("Impossible d'initialiser le gestionnaire de notifications: Pas de page active");
                    return;
                }

                // Créer le gestionnaire de notifications
                _notificationManager = new NotificationManager();

                // L'ajouter à la page (par-dessus tout)
                if (currentPage.Content is Layout layout)
                {
                    if (layout is Grid grid)
                    {
                        // Grid: ajouter par-dessus tout
                        grid.Children.Add(_notificationManager);
                    }
                    else if (layout is AbsoluteLayout absoluteLayout)
                    {
                        // AbsoluteLayout: ajouter par-dessus
                        absoluteLayout.Children.Add(_notificationManager);
                    }
                    else
                    {
                        // Autres layouts: wrapper dans un Grid
                        var wrapper = new Grid();
                        var parent = layout.Parent;

                        if (parent is ContentPage page)
                        {
                            page.Content = wrapper;
                        }

                        wrapper.Children.Add(layout);
                        wrapper.Children.Add(_notificationManager);
                    }
                }
                else
                {
                    // Pas de layout: créer un Grid wrapper
                    var wrapper = new Grid();
                    var oldContent = currentPage.Content;
                    currentPage.Content = wrapper;

                    if (oldContent != null)
                    {
                        wrapper.Children.Add(oldContent);
                    }

                    wrapper.Children.Add(_notificationManager);
                }

                _isInitialized = true;
                
#if DEBUG
                _logger.LogDebug("✅ Gestionnaire de notifications initialisé");
#endif
            });
        }

        public async Task ShowSuccessAsync(string message, string? title = null)
        {
            await ShowNotificationAsync(title ?? "Succès", message, NotificationType.Success);
        }

        public async Task ShowInfoAsync(string message, string? title = null)
        {
            await ShowNotificationAsync(title ?? "Information", message, NotificationType.Info);
        }

        public async Task ShowWarningAsync(string message, string? title = null)
        {
            await ShowNotificationAsync(title ?? "Attention", message, NotificationType.Warning);
        }

        public async Task ShowErrorAsync(string message, string? title = null)
        {
            await ShowNotificationAsync(title ?? "Erreur", message, NotificationType.Error);
        }

        public async Task ShowForecastCreatedAsync(Models.WeatherForecast forecast)
        {
            var title = "Nouvelle Prévision";
            var message = $"{forecast.Date:dd/MM/yyyy} - {forecast.TemperatureC}°C - {forecast.Summary}";
            await ShowNotificationAsync(title, message, NotificationType.Success);
        }

        public async Task ShowForecastUpdatedAsync(Models.WeatherForecast forecast)
        {
            var title = "Prévision Modifiée";
            var message = $"{forecast.Date:dd/MM/yyyy} - {forecast.TemperatureC}°C - {forecast.Summary}";
            await ShowNotificationAsync(title, message, NotificationType.Info);
        }

        public async Task ShowForecastDeletedAsync(int forecastId)
        {
            var title = "Prévision Supprimée";
            var message = $"La prévision #{forecastId} a été supprimée";
            await ShowNotificationAsync(title, message, NotificationType.Warning);
        }

        private async Task ShowNotificationAsync(string title, string message, NotificationType type, int durationMs = 5000)
        {
            try
            {
                await EnsureInitializedAsync();

                if (_notificationManager != null)
                {
                    await _notificationManager.ShowNotificationAsync(title, message, type, durationMs);
                    
#if DEBUG
                    _logger.LogDebug("📢 MOBILE - Notification affichée: {Title} - {Message}", title, message);
#endif
                }
                else
                {
                    _logger.LogWarning("Gestionnaire de notifications non disponible");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage de la notification: {Title} - {Message}", title, message);
            }
        }

        private ContentPage? GetCurrentPage()
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
