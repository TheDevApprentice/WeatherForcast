namespace mobile.Services
{
    /// <summary>
    /// Service pour afficher des notifications visuelles (toasts, alerts)
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Affiche une notification toast de succès
        /// </summary>
        Task ShowSuccessAsync(string message, string? title = null);

        /// <summary>
        /// Affiche une notification toast d'information
        /// </summary>
        Task ShowInfoAsync(string message, string? title = null);

        /// <summary>
        /// Affiche une notification toast d'avertissement
        /// </summary>
        Task ShowWarningAsync(string message, string? title = null);

        /// <summary>
        /// Affiche une notification toast d'erreur
        /// </summary>
        Task ShowErrorAsync(string message, string? title = null);

        /// <summary>
        /// Affiche une notification pour un forecast créé
        /// </summary>
        Task ShowForecastCreatedAsync(Models.WeatherForecast forecast);

        /// <summary>
        /// Affiche une notification pour un forecast mis à jour
        /// </summary>
        Task ShowForecastUpdatedAsync(Models.WeatherForecast forecast);

        /// <summary>
        /// Affiche une notification pour un forecast supprimé
        /// </summary>
        Task ShowForecastDeletedAsync(int forecastId);

        /// <summary>
        /// Réinitialise le gestionnaire de notifications (force la recréation)
        /// </summary>
        void Reset();
    }
}
