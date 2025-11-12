using mobile.Services.Stores;

namespace mobile.Services.External.Interfaces
{
    /// <summary>
    /// Service de messagerie instantanée
    /// Gère l'envoi et la réception de messages en temps réel
    /// </summary>
    public interface IMessengerService
    {
        /// <summary>
        /// Événement déclenché lors de la réception d'un nouveau message
        /// </summary>
        event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        /// <summary>
        /// Événement déclenché lors d'un changement de statut de connexion
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// Indique si le service est connecté au serveur de messagerie
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Initialise la connexion au serveur de messagerie
        /// </summary>
        /// <param name="userId">ID de l'utilisateur connecté</param>
        /// <param name="accessToken">Token d'authentification</param>
        Task<bool> ConnectAsync(string userId, string accessToken);

        /// <summary>
        /// Déconnecte du serveur de messagerie
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Envoie un message dans une conversation
        /// </summary>
        /// <param name="conversationId">ID de la conversation</param>
        /// <param name="message">Message à envoyer</param>
        /// <returns>True si l'envoi a réussi</returns>
        Task<bool> SendMessageAsync(string conversationId, Message message);

        /// <summary>
        /// Marque un message comme lu
        /// </summary>
        /// <param name="conversationId">ID de la conversation</param>
        /// <param name="messageId">ID du message</param>
        Task MarkMessageAsReadAsync(string conversationId, string messageId);

        /// <summary>
        /// Marque tous les messages d'une conversation comme lus
        /// </summary>
        /// <param name="conversationId">ID de la conversation</param>
        Task MarkConversationAsReadAsync(string conversationId);

        /// <summary>
        /// Indique qu'un utilisateur est en train de taper dans une conversation
        /// </summary>
        /// <param name="conversationId">ID de la conversation</param>
        Task SendTypingIndicatorAsync(string conversationId);

        /// <summary>
        /// Crée une nouvelle conversation
        /// </summary>
        /// <param name="conversation">Conversation à créer</param>
        /// <returns>La conversation créée avec son ID serveur</returns>
        Task<Conversation?> CreateConversationAsync(Conversation conversation);

        /// <summary>
        /// Récupère l'historique des messages d'une conversation
        /// </summary>
        /// <param name="conversationId">ID de la conversation</param>
        /// <param name="limit">Nombre maximum de messages à récupérer</param>
        /// <param name="before">Récupérer les messages avant cette date (pour pagination)</param>
        Task<List<Message>> GetMessageHistoryAsync(string conversationId, int limit = 50, DateTime? before = null);

        /// <summary>
        /// Récupère toutes les conversations de l'utilisateur
        /// </summary>
        Task<List<Conversation>> GetConversationsAsync();
    }

    /// <summary>
    /// Arguments de l'événement MessageReceived
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        public string ConversationId { get; set; } = string.Empty;
        public Message Message { get; set; } = null!;
    }

    /// <summary>
    /// Arguments de l'événement ConnectionStatusChanged
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
