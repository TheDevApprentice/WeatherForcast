using Microsoft.Maui.Layouts;
using mobile.Controls;
using mobile.Services.Notifications.Interfaces;
using mobile.Services.Stores;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

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

                // Créer un wrapper AbsoluteLayout invisible pour le manager
                // Cela garantit que le manager ne prend pas d'espace dans le layout
                var absoluteWrapper = new AbsoluteLayout
                {
                    InputTransparent = true,
                    BackgroundColor = Colors.Transparent,
                    IsVisible = true
                };
                
                // Ajouter le manager au wrapper en position absolue (remplit tout l'espace)
                absoluteWrapper.Children.Add(overlayControl);
                AbsoluteLayout.SetLayoutBounds(overlayControl, new Rect(0, 0, 1, 1));
                AbsoluteLayout.SetLayoutFlags(overlayControl, AbsoluteLayoutFlags.All);

                // L'ajouter à la page (par-dessus tout en overlay)
                if (currentPage.Content is Layout layout)
                {
                    if (layout is Grid grid)
                    {
                        // Grid: ajouter le wrapper AbsoluteLayout par-dessus tout
                        // Utiliser Row/Column spanning pour ne pas prendre d'espace
                        grid.Children.Add(absoluteWrapper);
                        Grid.SetRow(absoluteWrapper, 0);
                        Grid.SetColumn(absoluteWrapper, 0);
                        Grid.SetRowSpan(absoluteWrapper, int.MaxValue);
                        Grid.SetColumnSpan(absoluteWrapper, int.MaxValue);
                    }
                    else if (layout is AbsoluteLayout absoluteLayout)
                    {
                        // AbsoluteLayout: ajouter le wrapper par-dessus
                        absoluteLayout.Children.Add(absoluteWrapper);
                        AbsoluteLayout.SetLayoutBounds(absoluteWrapper, new Rect(0, 0, 1, 1));
                        AbsoluteLayout.SetLayoutFlags(absoluteWrapper, AbsoluteLayoutFlags.All);
                    }
                    else
                    {
                        // Autres layouts: wrapper dans un AbsoluteLayout overlay
                        var rootAbsolute = new AbsoluteLayout
                        {
                            BackgroundColor = Colors.Transparent
                        };
                        
                        // Ajouter le contenu existant
                        rootAbsolute.Children.Add(layout);
                        AbsoluteLayout.SetLayoutBounds(layout, new Rect(0, 0, 1, 1));
                        AbsoluteLayout.SetLayoutFlags(layout, AbsoluteLayoutFlags.All);
                        
                        // Ajouter le wrapper des notifications par-dessus
                        rootAbsolute.Children.Add(absoluteWrapper);
                        AbsoluteLayout.SetLayoutBounds(absoluteWrapper, new Rect(0, 0, 1, 1));
                        AbsoluteLayout.SetLayoutFlags(absoluteWrapper, AbsoluteLayoutFlags.All);
                        
                        // Remplacer le contenu de la page
                        currentPage.Content = rootAbsolute;
                    }
                }
                else
                {
                    // Pas de layout: créer un AbsoluteLayout wrapper overlay
                    var rootAbsolute = new AbsoluteLayout
                    {
                        BackgroundColor = Colors.Transparent
                    };
                    
                    var oldContent = currentPage.Content;
                    if (oldContent != null)
                    {
                        rootAbsolute.Children.Add(oldContent);
                        AbsoluteLayout.SetLayoutBounds(oldContent, new Rect(0, 0, 1, 1));
                        AbsoluteLayout.SetLayoutFlags(oldContent, AbsoluteLayoutFlags.All);
                    }

                    // Ajouter le wrapper des notifications par-dessus
                    rootAbsolute.Children.Add(absoluteWrapper);
                    AbsoluteLayout.SetLayoutBounds(absoluteWrapper, new Rect(0, 0, 1, 1));
                    AbsoluteLayout.SetLayoutFlags(absoluteWrapper, AbsoluteLayoutFlags.All);
                    
                    currentPage.Content = rootAbsolute;
                }

                // NotificationManager initialisé sur la page avec positionnement absolu
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

                // Thread-safe: ajouter au store sur le thread UI
                // await MainThread.InvokeOnMainThreadAsync(() =>
                // {
                //     _notificationStore.AddNotification(notification);
                //     // Notification ajoutée au store
                // });

#if ANDROID || IOS
                // Affichage mobile : Snackbar en bas de l'écran
                await ShowMobileNotificationAsync(title, message, type, durationMs);
#else
                // Afficher la notification à l'écran (desktop : overlay en haut à droite)
                await EnsureInitializedAsync();

                if (_notificationManager != null)
                {
                    // NotificationManager disponible, affichage en cours
                    // Thread-safe: afficher et marquer sur le thread UI
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await _notificationManager.ShowNotificationAsync(title, message, type, durationMs);

                        // Marquer comme affichée et lue
                        notification.WasDisplayed = true;
                        _notificationStore.MarkAsRead(notification.Id);
                        // Notification affichée avec succès et marquée comme lue
                    });
                }
                else
                {
                    // Gestionnaire de notifications non disponible - notification stockée mais non affichée
                }
#endif
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

#if ANDROID || IOS
        /// <summary>
        /// Affichage des notifications sur mobile via Snackbar (CommunityToolkit)
        /// </summary>
        private async Task ShowMobileNotificationAsync (string title, string message, NotificationType type, int durationMs)
        {
            var text = string.IsNullOrWhiteSpace(title)
                ? message
                : $"{title}\n{message}";

            var options = GetSnackbarOptions(type);

            var snackbar = Snackbar.Make(
                text,
                duration: TimeSpan.FromMilliseconds(durationMs),
                visualOptions: options);

            await snackbar.Show();
        }

        /// <summary>
        /// Style du Snackbar selon le type de notification
        /// </summary>
        private static SnackbarOptions GetSnackbarOptions (NotificationType type)
        {
            var background = type switch
            {
                NotificationType.Success => Color.FromArgb("#10B981"), // Vert
                NotificationType.Error   => Color.FromArgb("#EF4444"), // Rouge
                NotificationType.Warning => Color.FromArgb("#F59E0B"), // Orange
                NotificationType.Info    => Color.FromArgb("#3B82F6"), // Bleu
                _                        => Color.FromArgb("#374151")  // Gris neutre
            };

            return new SnackbarOptions
            {
                BackgroundColor = background,
                TextColor = Colors.White,
                CornerRadius = new CornerRadius(12),
                Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14)
            };
        }
#endif
    }
}
