namespace mobile.Services
{
    /// <summary>
    /// Service pour les notifications push (Firebase/APNS)
    /// Permet d'envoyer des notifications même quand l'app est fermée
    /// </summary>
    public interface IPushNotificationService
    {
        /// <summary>
        /// Initialise le service de notifications push
        /// Demande les permissions et enregistre le device token
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Obtient le token du device pour les notifications push
        /// </summary>
        Task<string?> GetDeviceTokenAsync();

        /// <summary>
        /// Enregistre le device token sur le serveur
        /// </summary>
        Task RegisterDeviceTokenAsync(string userId, string deviceToken);

        /// <summary>
        /// Désenregistre le device token (lors de la déconnexion)
        /// </summary>
        Task UnregisterDeviceTokenAsync(string userId);

        /// <summary>
        /// Vérifie si les permissions sont accordées
        /// </summary>
        Task<bool> HasPermissionAsync();

        /// <summary>
        /// Demande les permissions pour les notifications push
        /// </summary>
        Task<bool> RequestPermissionAsync();

        /// <summary>
        /// Envoie une notification push à un utilisateur spécifique
        /// (Appelé depuis le serveur/API)
        /// </summary>
        Task SendNotificationAsync(
            string userId,
            string title,
            string message,
            Dictionary<string, string>? data = null);

        /// <summary>
        /// Envoie une notification push à plusieurs utilisateurs
        /// </summary>
        Task SendNotificationToMultipleAsync(
            List<string> userIds,
            string title,
            string message,
            Dictionary<string, string>? data = null);

        /// <summary>
        /// Appelé quand une notification push est reçue
        /// </summary>
        event EventHandler<PushNotificationReceivedEventArgs>? NotificationReceived;

        /// <summary>
        /// Appelé quand l'utilisateur clique sur une notification
        /// </summary>
        event EventHandler<PushNotificationTappedEventArgs>? NotificationTapped;
    }

    /// <summary>
    /// Arguments pour l'événement NotificationReceived
    /// </summary>
    public class PushNotificationReceivedEventArgs : EventArgs
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Data { get; set; } = new();
    }

    /// <summary>
    /// Arguments pour l'événement NotificationTapped
    /// </summary>
    public class PushNotificationTappedEventArgs : EventArgs
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Data { get; set; } = new();
    }
}
