using mobile.Controls;

namespace mobile.Models
{
    /// <summary>
    /// Modèle représentant une notification dans le système
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Identifiant unique de la notification
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Titre de la notification
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Message de la notification
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Type de notification (Success, Error, Warning, Info)
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// Date et heure de création de la notification
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Indique si la notification a été lue/vue
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Indique si la notification a été affichée à l'écran
        /// </summary>
        public bool WasDisplayed { get; set; } = false;
    }
}
