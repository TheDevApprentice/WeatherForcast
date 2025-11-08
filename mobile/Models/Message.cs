using mobile.Controls;

namespace mobile.Models
{
    /// <summary>
    /// Modèle représentant un message dans le système
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Identifiant unique du message
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Titre du message
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Message du message
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Type de message (Success, Error, Warning, Info)
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// Date et heure de création du message
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Indique si le message a été lue/vue
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// Indique si le message a été affichée à l'écran
        /// </summary>
        public bool WasDisplayed { get; set; } = false;
    }
}
