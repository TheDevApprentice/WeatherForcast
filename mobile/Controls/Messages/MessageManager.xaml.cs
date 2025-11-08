using System.Collections.Concurrent;

namespace mobile.Controls
{
    /// <summary>
    /// Gestionnaire de notifications avec queue et animations
    /// Affiche les messages en haut à droite avec réorganisation automatique
    /// </summary>
    public partial class MessageManager : AbsoluteLayout
    {
        private static MessageManager? _instance;
        private readonly ConcurrentDictionary<string, MessageCard> _activeMessages = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private const int MaxMessages = 5; // Maximum de messages simultanées

        public MessageManager ()
        {
            InitializeComponent();
            _instance = this;
        }

        /// <summary>
        /// Obtient l'instance du gestionnaire
        /// </summary>
        public static MessageManager? Instance => _instance;

        /// <summary>
        /// Affiche un message
        /// </summary>
        public async Task ShowMessageAsync (
            string title,
            string content,
            MessageType type,
            int durationMs = 5000)
        {
            await _semaphore.WaitAsync();

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Limiter le nombre de messages
                    if (_activeMessages.Count >= MaxMessages)
                    {
                        // Retirer le plus ancien
                        var oldest = _activeMessages.First();
                        await RemoveMessageAsync(oldest.Key);
                    }

                    // Créer le message
                    var message = new MessageCard();
                    message.Closed += async (s, e) => await OnMessageClosed(message);

                    // Ajouter au stack
                    MessageStack.Children.Add(message);
                    _activeMessages.TryAdd(message.MessageId, message);

                    // Afficher avec animation
                    await message.ShowAsync(title, content, type, durationMs);
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
        private async Task OnMessageClosed (MessageCard message)
        {
            await _semaphore.WaitAsync();

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Retirer de la liste
                    _activeMessages.TryRemove(message.MessageId, out _);

                    // Retirer du stack
                    MessageStack.Children.Remove(message);

                    // Nettoyer
                    message.Dispose();

                    // Réorganiser les notifications restantes avec animation
                    await ReorganizeMessagesAsync();
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
        private async Task ReorganizeMessagesAsync ()
        {
            // Les notifications se réorganisent automatiquement grâce au VerticalStackLayout
            // On peut ajouter une animation de "bounce" pour un effet plus fluide
            var tasks = new List<Task>();

            foreach (var child in MessageStack.Children)
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
        private async Task RemoveMessageAsync (string messageId)
        {
            if (_activeMessages.TryGetValue(messageId, out var message))
            {
                message.Dispose();
                await OnMessageClosed(message);
            }
        }

        /// <summary>
        /// Ferme toutes les notifications
        /// </summary>
        public async Task ClearAllAsync ()
        {
            await _semaphore.WaitAsync();

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var tasks = _activeMessages.Values
                        .Select(n => Task.Run(() => n.Dispose()))
                        .ToList();

                    await Task.WhenAll(tasks);

                    _activeMessages.Clear();
                    MessageStack.Children.Clear();
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
