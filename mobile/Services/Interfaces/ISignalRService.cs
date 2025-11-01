namespace mobile.Services
{
    /// <summary>
    /// Interface pour la gestion des connexions SignalR
    /// </summary>
    public interface ISignalRService
    {
        /// <summary>
        /// Indique si le service est connecté
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Événement déclenché lors de la création d'un forecast
        /// </summary>
        event EventHandler<Models.WeatherForecast>? ForecastCreated;

        /// <summary>
        /// Événement déclenché lors de la mise à jour d'un forecast
        /// </summary>
        event EventHandler<Models.WeatherForecast>? ForecastUpdated;

        /// <summary>
        /// Événement déclenché lors de la suppression d'un forecast
        /// </summary>
        event EventHandler<int>? ForecastDeleted;

        /// <summary>
        /// Événement déclenché lors de l'envoi d'un email à l'utilisateur
        /// </summary>
        event EventHandler<EmailNotification>? EmailSent;

        /// <summary>
        /// Événement déclenché lors de l'envoi d'un email de vérification
        /// </summary>
        event EventHandler<EmailNotification>? VerificationEmailSent;

        /// <summary>
        /// Démarre la connexion au hub Users (toujours actif)
        /// </summary>
        Task StartUsersHubAsync(string? email = null);

        /// <summary>
        /// Démarre la connexion au hub WeatherForecast
        /// </summary>
        Task StartForecastHubAsync();

        /// <summary>
        /// Arrête la connexion au hub WeatherForecast
        /// </summary>
        Task StopForecastHubAsync();

        /// <summary>
        /// Arrête toutes les connexions SignalR
        /// </summary>
        Task StopAllAsync();

        /// <summary>
        /// Rejoint le canal email (pour les utilisateurs non authentifiés)
        /// </summary>
        Task JoinEmailChannelAsync(string email);

        /// <summary>
        /// Quitte le canal email
        /// </summary>
        Task LeaveEmailChannelAsync(string email);
    }

    /// <summary>
    /// Notification d'email
    /// </summary>
    public class EmailNotification
    {
        public string? Subject { get; set; }
        public string? Message { get; set; }
        public string? CorrelationId { get; set; }
    }
}
