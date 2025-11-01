using System.Collections.Concurrent;

namespace mobile.Controls
{
    /// <summary>
    /// Gestionnaire de notifications avec queue et animations
    /// Affiche les notifications en haut à droite avec réorganisation automatique
    /// </summary>
    public partial class NotificationManager : AbsoluteLayout
    {
        private static NotificationManager? _instance;
        private readonly ConcurrentDictionary<string, NotificationCard> _activeNotifications = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private const int MaxNotifications = 5; // Maximum de notifications simultanées

        public NotificationManager()
        {
            InitializeComponent();
            _instance = this;
        }

        /// <summary>
        /// Obtient l'instance du gestionnaire
        /// </summary>
        public static NotificationManager? Instance => _instance;

        /// <summary>
        /// Affiche une notification
        /// </summary>
        public async Task ShowNotificationAsync(
            string title,
            string message,
            NotificationType type,
            int durationMs = 5000)
        {
            await _semaphore.WaitAsync();

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Limiter le nombre de notifications
                    if (_activeNotifications.Count >= MaxNotifications)
                    {
                        // Retirer la plus ancienne
                        var oldest = _activeNotifications.First();
                        await RemoveNotificationAsync(oldest.Key);
                    }

                    // Créer la notification
                    var notification = new NotificationCard();
                    notification.Closed += async (s, e) => await OnNotificationClosed(notification);

                    // Ajouter au stack
                    NotificationStack.Children.Add(notification);
                    _activeNotifications.TryAdd(notification.NotificationId, notification);

                    // Afficher avec animation
                    await notification.ShowAsync(title, message, type, durationMs);
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Appelé quand une notification se ferme
        /// </summary>
        private async Task OnNotificationClosed(NotificationCard notification)
        {
            await _semaphore.WaitAsync();

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Retirer de la liste
                    _activeNotifications.TryRemove(notification.NotificationId, out _);

                    // Retirer du stack
                    NotificationStack.Children.Remove(notification);

                    // Nettoyer
                    notification.Dispose();

                    // Réorganiser les notifications restantes avec animation
                    await ReorganizeNotificationsAsync();
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Réorganise les notifications avec animation fluide
        /// </summary>
        private async Task ReorganizeNotificationsAsync()
        {
            // Les notifications se réorganisent automatiquement grâce au VerticalStackLayout
            // On peut ajouter une animation de "bounce" pour un effet plus fluide
            var tasks = new List<Task>();

            foreach (var child in NotificationStack.Children)
            {
                if (child is NotificationCard card)
                {
                    // Petit effet de "bounce" pour indiquer le réarrangement
                    tasks.Add(Task.Run(async () =>
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await card.ScaleTo(0.98, 100, Easing.CubicOut);
                            await card.ScaleTo(1.0, 100, Easing.CubicOut);
                        });
                    }));
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Retire une notification spécifique
        /// </summary>
        private async Task RemoveNotificationAsync(string notificationId)
        {
            if (_activeNotifications.TryGetValue(notificationId, out var notification))
            {
                notification.Dispose();
                await OnNotificationClosed(notification);
            }
        }

        /// <summary>
        /// Ferme toutes les notifications
        /// </summary>
        public async Task ClearAllAsync()
        {
            await _semaphore.WaitAsync();

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var tasks = _activeNotifications.Values
                        .Select(n => Task.Run(() => n.Dispose()))
                        .ToList();

                    await Task.WhenAll(tasks);

                    _activeNotifications.Clear();
                    NotificationStack.Children.Clear();
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
