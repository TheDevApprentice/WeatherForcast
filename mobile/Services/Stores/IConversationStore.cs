using mobile.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace mobile
{
    /// <summary>
    /// Interface pour le store de conversations
    /// Gère les conversations de l'utilisateur avec support de notifications
    /// </summary>
    public interface IConversationStore : INotifyPropertyChanged
    {
        /// <summary>
        /// Collection observable de toutes les conversations
        /// </summary>
        ObservableCollection<Conversation> Conversations { get; }

        /// <summary>
        /// Nombre total de messages non lus dans toutes les conversations
        /// </summary>
        int TotalUnreadCount { get; }

        /// <summary>
        /// Obtient la conversation de support (toujours présente et épinglée)
        /// </summary>
        Conversation SupportConversation { get; }

        /// <summary>
        /// Ajoute ou met à jour une conversation
        /// </summary>
        void AddOrUpdateConversation(Conversation conversation);

        /// <summary>
        /// Ajoute un message à une conversation
        /// </summary>
        void AddMessageToConversation(string conversationId, Message message);

        /// <summary>
        /// Marque tous les messages d'une conversation comme lus
        /// </summary>
        void MarkConversationAsRead(string conversationId);

        /// <summary>
        /// Marque toutes les conversations comme lues
        /// </summary>
        void MarkAllAsRead();

        /// <summary>
        /// Supprime une conversation
        /// </summary>
        void RemoveConversation(string conversationId);

        /// <summary>
        /// Obtient une conversation par son ID
        /// </summary>
        Conversation? GetConversation(string conversationId);

        /// <summary>
        /// Crée une nouvelle conversation directe avec un utilisateur
        /// </summary>
        Conversation CreateDirectConversation(ConversationMember otherMember, string currentUserId);

        /// <summary>
        /// Efface toutes les conversations (sauf support)
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Initialise le store avec la conversation de support
        /// </summary>
        void Initialize(string currentUserId, string currentUserDisplayName);
    }
}
