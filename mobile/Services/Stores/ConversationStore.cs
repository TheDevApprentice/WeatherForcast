using mobile.Controls;
using mobile.Services.External.Interfaces;
using mobile.Services.Internal.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace mobile.Services.Stores
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
        void AddOrUpdateConversation (Conversation conversation);

        /// <summary>
        /// Ajoute un message à une conversation
        /// </summary>
        void AddMessageToConversation (string conversationId, Message message);

        /// <summary>
        /// Marque tous les messages d'une conversation comme lus
        /// </summary>
        void MarkConversationAsRead (string conversationId);

        /// <summary>
        /// Marque toutes les conversations comme lues
        /// </summary>
        void MarkAllAsRead ();

        /// <summary>
        /// Supprime une conversation
        /// </summary>
        void RemoveConversation (string conversationId);

        /// <summary>
        /// Obtient une conversation par son ID
        /// </summary>
        Conversation? GetConversation (string conversationId);

        /// <summary>
        /// Crée une nouvelle conversation directe avec un utilisateur
        /// </summary>
        Conversation CreateDirectConversation (ConversationMember otherMember, string currentUserId);

        /// <summary>
        /// Efface toutes les conversations (sauf support)
        /// </summary>
        void ClearAll ();

        /// <summary>
        /// Initialise le store avec la conversation de support
        /// </summary>
        void Initialize (string currentUserId, string currentUserDisplayName);

        /// <summary>
        /// Obtient les conversations à afficher dans le hub de notifications
        /// (conversations épinglées + conversations avec messages non lus)
        /// </summary>
        List<Conversation> GetNotificationConversations ();

        /// <summary>
        /// Envoie un message dans une conversation (via MessengerService)
        /// </summary>
        Task<bool> SendMessageAsync(string conversationId, Message message);

        /// <summary>
        /// Marque une conversation comme lue (localement et sur le serveur via MessengerService)
        /// </summary>
        Task MarkConversationAsReadAsync(string conversationId);

        /// <summary>
        /// Récupère l'ID de l'utilisateur actuel
        /// </summary>
        string GetCurrentUserId();
    }

    /// <summary>
    /// Implémentation du store de conversations
    /// </summary>
    public class ConversationStore : IConversationStore
    {
        private readonly ObservableCollection<Conversation> _conversations = new();
        private readonly IMessengerService _messengerService;
        private readonly ISecureStorageService _secureStorage;
        private Conversation? _supportConversation;
        private string _currentUserId = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ConversationStore(
            IMessengerService messengerService,
            ISecureStorageService secureStorage)
        {
            _messengerService = messengerService;
            _secureStorage = secureStorage;

            // S'abonner aux événements du MessengerService
            _messengerService.MessageReceived += OnMessageReceivedFromServer;
        }

        /// <summary>
        /// Collection observable de toutes les conversations triées
        /// </summary>
        public ObservableCollection<Conversation> Conversations => _conversations;

        /// <summary>
        /// Nombre total de messages non lus
        /// </summary>
        public int TotalUnreadCount => _conversations.Sum(c => c.UnreadCount);

        /// <summary>
        /// Conversation de support (toujours présente et épinglée)
        /// </summary>
        public Conversation SupportConversation => _supportConversation
            ?? throw new InvalidOperationException("Support conversation not initialized");

        /// <summary>
        /// Gestionnaire pour les messages reçus du serveur
        /// </summary>
        private void OnMessageReceivedFromServer(object? sender, MessageReceivedEventArgs e)
        {
            // Ajouter le message à la conversation locale
            AddMessageToConversation(e.ConversationId, e.Message);
        }

        /// <summary>
        /// Initialise le store avec la conversation de support
        /// </summary>
        public void Initialize (string currentUserId, string currentUserDisplayName)
        {
            _currentUserId = currentUserId;

            // Créer la conversation de support si elle n'existe pas
            if (_supportConversation == null)
            {
                _supportConversation = new Conversation
                {
                    Id = "support",
                    Title = "Support",
                    Type = ConversationType.Support,
                    IsPinned = true,
                    CreatedAt = DateTime.Now,
                    Members = new List<ConversationMember>
                    {
                        new ConversationMember
                        {
                            UserId = currentUserId,
                            DisplayName = currentUserDisplayName,
                            Role = ConversationRole.Member
                        },
                        new ConversationMember
                        {
                            UserId = "support",
                            DisplayName = "Équipe Support",
                            FirstName = "Support",
                            LastName = "Team",
                            Role = ConversationRole.Support
                        }
                    },
                    Messages = new List<Message>
                    {
                        new Message
                        {
                            Id = Guid.NewGuid().ToString(),
                            Title = "Bienvenue",
                            Content = "Bonjour ! Nous sommes là pour vous aider. N'hésitez pas à nous poser vos questions.",
                            Type = MessageType.Info,
                            Timestamp = DateTime.Now,
                            IsRead = false,
                            WasDisplayed = false
                        }
                    }
                };

                _conversations.Add(_supportConversation);
                SortConversations();
            }
        }

        /// <summary>
        /// Ajoute ou met à jour une conversation
        /// </summary>
        public void AddOrUpdateConversation (Conversation conversation)
        {
            var existing = _conversations.FirstOrDefault(c => c.Id == conversation.Id);
            if (existing != null)
            {
                var index = _conversations.IndexOf(existing);
                _conversations[index] = conversation;
            }
            else
            {
                _conversations.Add(conversation);
            }

            SortConversations();
            OnPropertyChanged(nameof(TotalUnreadCount));
        }

        /// <summary>
        /// Ajoute un message à une conversation
        /// </summary>
        public void AddMessageToConversation (string conversationId, Message message)
        {
            var conversation = GetConversation(conversationId);
            if (conversation != null)
            {
                conversation.Messages.Add(message);

                // Notifier les changements sur la conversation
                conversation.NotifyPropertyChanged(nameof(conversation.Messages));

                SortConversations();
                OnPropertyChanged(nameof(TotalUnreadCount));
            }
        }

        /// <summary>
        /// Marque tous les messages d'une conversation comme lus
        /// </summary>
        public void MarkConversationAsRead (string conversationId)
        {
            var conversation = GetConversation(conversationId);
            if (conversation != null)
            {
                foreach (var message in conversation.Messages.Where(m => !m.IsRead))
                {
                    message.IsRead = true;
                }

                // Notifier les changements sur la conversation
                conversation.NotifyPropertyChanged(nameof(conversation.Messages));
                OnPropertyChanged(nameof(TotalUnreadCount));
            }
        }

        /// <summary>
        /// Marque toutes les conversations comme lues
        /// </summary>
        public void MarkAllAsRead ()
        {
            foreach (var conversation in _conversations)
            {
                foreach (var message in conversation.Messages.Where(m => !m.IsRead))
                {
                    message.IsRead = true;
                }

                // Notifier les changements sur chaque conversation
                conversation.NotifyPropertyChanged(nameof(conversation.Messages));
            }
            OnPropertyChanged(nameof(TotalUnreadCount));
        }

        /// <summary>
        /// Supprime une conversation (sauf support)
        /// </summary>
        public void RemoveConversation (string conversationId)
        {
            if (conversationId == "support")
                return; // Ne pas supprimer la conversation de support

            var conversation = GetConversation(conversationId);
            if (conversation != null)
            {
                _conversations.Remove(conversation);
                OnPropertyChanged(nameof(TotalUnreadCount));
            }
        }

        /// <summary>
        /// Obtient une conversation par son ID
        /// </summary>
        public Conversation? GetConversation (string conversationId)
        {
            return _conversations.FirstOrDefault(c => c.Id == conversationId);
        }

        /// <summary>
        /// Crée une nouvelle conversation directe
        /// </summary>
        public Conversation CreateDirectConversation (ConversationMember otherMember, string currentUserId)
        {
            var conversation = new Conversation
            {
                Type = ConversationType.Direct,
                Members = new List<ConversationMember>
                {
                    new ConversationMember
                    {
                        UserId = currentUserId,
                        DisplayName = "Moi"
                    },
                    otherMember
                }
            };

            AddOrUpdateConversation(conversation);
            return conversation;
        }

        /// <summary>
        /// Efface toutes les conversations sauf la conversation de support
        /// </summary>
        public void ClearAll ()
        {
            var conversationsToRemove = _conversations
                .Where(c => c.Id != "support")
                .ToList();

            foreach (var conversation in conversationsToRemove)
            {
                _conversations.Remove(conversation);
            }
        }

        /// <summary>
        /// Obtient les conversations à afficher dans le hub de notifications
        /// (conversations épinglées + conversations avec messages non lus)
        /// </summary>
        public List<Conversation> GetNotificationConversations ()
        {
            return _conversations
                .Where(c => c.IsPinned || c.UnreadCount > 0)
                .OrderByDescending(c => c.IsPinned)
                .ThenByDescending(c => c.LastActivity)
                .ToList();
        }

        /// <summary>
        /// Envoie un message dans une conversation (via MessengerService)
        /// </summary>
        public async Task<bool> SendMessageAsync(string conversationId, Message message)
        {
            return await _messengerService.SendMessageAsync(conversationId, message);
        }

        /// <summary>
        /// Marque une conversation comme lue (localement et sur le serveur via MessengerService)
        /// </summary>
        public async Task MarkConversationAsReadAsync(string conversationId)
        {
            // Marquer localement d'abord
            MarkConversationAsRead(conversationId);

            // Puis notifier le serveur
            await _messengerService.MarkConversationAsReadAsync(conversationId);
        }

        /// <summary>
        /// Récupère l'ID de l'utilisateur actuel
        /// </summary>
        public string GetCurrentUserId()
        {
            return _currentUserId;
        }

        /// <summary>
        /// Trie les conversations : support épinglé en premier, puis par dernière activité
        /// </summary>
        private void SortConversations ()
        {
            var sorted = _conversations
                .OrderByDescending(c => c.IsPinned)
                .ThenByDescending(c => c.LastActivity)
                .ToList();

            _conversations.Clear();
            foreach (var conversation in sorted)
            {
                _conversations.Add(conversation);
            }
        }

        protected void OnPropertyChanged ([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
