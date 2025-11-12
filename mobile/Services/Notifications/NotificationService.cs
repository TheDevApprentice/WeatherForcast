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
                    // Impossible d'initialiser le gestionnaire de notifications: Pas de page active
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

                // NotificationManager initialisé sur la page
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
                // Tentative d'affichage notification
                // Créer la notification et l'ajouter au store
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    WasDisplayed = false
                };

                _notificationStore.AddNotification(notification);
                // Notification ajoutée au store

                // Afficher la notification à l'écran (desktop uniquement)
                await EnsureInitializedAsync();

                if (_notificationManager != null)
                {
                    // NotificationManager disponible, affichage en cours
                    await _notificationManager.ShowNotificationAsync(title, message, type, durationMs);

                    // Marquer comme affichée et lue
                    notification.WasDisplayed = true;
                    _notificationStore.MarkAsRead(notification.Id);
                    // Notification affichée avec succès et marquée comme lue
                }
                else
                {
                    // Gestionnaire de notifications non disponible - notification stockée mais non affichée
                }
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
