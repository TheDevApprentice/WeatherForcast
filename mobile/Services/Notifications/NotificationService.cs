using Microsoft.Maui.Layouts;
using mobile.Controls;
using mobile.Services.Notifications.Interfaces;
using mobile.Services.Stores;

namespace mobile.Services.Notifications
{
    /// <summary>
    /// Service de notification professionnel avec queue et animations
    /// Affiche les notifications en haut à droite (desktop uniquement)
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly INotificationStore _notificationStore;
        private NotificationManager? _notificationManager;

        public NotificationService (INotificationStore notificationStore)
        {
            _notificationStore = notificationStore;
        }

        private async Task EnsureInitializedAsync ()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_notificationManager != null && _notificationManager.Parent != null)
                {
                    return;
                }

                var currentPage = GetCurrentPage();
#if DEBUG
                var pageName = currentPage?.GetType().Name ?? "null";
                var contentName = currentPage?.Content?.GetType().Name ?? "null";
                _ = Shell.Current?.DisplayAlert("Debug Notification Init",
                    $"Page: {pageName}\nContent: {contentName}", "OK");
#endif
                if (currentPage == null)
                {
                    _notificationManager = null;
                    return;
                }

                var existingContent = currentPage.Content;

                // Si le contenu est déjà un AbsoluteLayout, essayer de trouver un NotificationManager existant
                if (existingContent is AbsoluteLayout existingAbsolute)
                {
                    NotificationManager? existingManager = null;

                    foreach (var child in existingAbsolute.Children)
                    {
                        if (child is NotificationManager manager)
                        {
                            existingManager = manager;
                            break;
                        }
                    }

                    if (existingManager != null)
                    {
                        _notificationManager = existingManager;
                        return;
                    }

                    _notificationManager = new NotificationManager();
                    existingAbsolute.Children.Add(_notificationManager);
                    AbsoluteLayout.SetLayoutBounds(_notificationManager, new Rect(0, 0, 1, 1));
                    AbsoluteLayout.SetLayoutFlags(_notificationManager, AbsoluteLayoutFlags.All);
                    return;
                }

                var rootAbsolute = new AbsoluteLayout
                {
                    BackgroundColor = Colors.Transparent
                };

                if (existingContent != null)
                {
                    currentPage.Content = null;
                    rootAbsolute.Children.Add(existingContent);
                    AbsoluteLayout.SetLayoutBounds(existingContent, new Rect(0, 0, 1, 1));
                    AbsoluteLayout.SetLayoutFlags(existingContent, AbsoluteLayoutFlags.All);
                }

                _notificationManager = new NotificationManager();
                rootAbsolute.Children.Add(_notificationManager);
                AbsoluteLayout.SetLayoutBounds(_notificationManager, new Rect(0, 0, 1, 1));
                AbsoluteLayout.SetLayoutFlags(_notificationManager, AbsoluteLayoutFlags.All);

                currentPage.Content = rootAbsolute;
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

        public async Task ShowForecastCreatedAsync (WeatherForecast forecast)
        {
            var title = "Nouvelle Prévision";
            var message = $"{forecast.Date:dd/MM/yyyy} - {forecast.TemperatureC}°C - {forecast.Summary}";
            await ShowNotificationAsync(title, message, NotificationType.Success);
        }

        public async Task ShowForecastUpdatedAsync (WeatherForecast forecast)
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
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    WasDisplayed = false
                };

                await EnsureInitializedAsync();

                if (_notificationManager == null)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
#if DEBUG
                    await Shell.Current.DisplayAlert("Debug NotificationService",
                        "Appel NotificationManager.ShowNotificationAsync", "OK");
#endif
                    await _notificationManager.ShowNotificationAsync(notification.Title, notification.Message, notification.Type, durationMs);

                    notification.WasDisplayed = true;
                    _notificationStore.MarkAsRead(notification.Id);
                });
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug NotificationService", $"❌ Erreur lors de l'affichage de la notification: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        /// <summary>
        /// Réinitialise le gestionnaire de notifications (force la recréation)
        /// </summary>
        public void Reset ()
        {
            // Réinitialisation du gestionnaire de notifications
            _notificationManager = null;
        }

        private ContentPage? GetCurrentPage ()
        {
            if (Shell.Current?.CurrentPage is ContentPage shellPage)
                return shellPage;

            var window = (Application.Current?.Windows?.Count ?? 0) > 0
                ? Application.Current!.Windows[0]
                : null;

            return window?.Page as ContentPage;
        }
    }
}
