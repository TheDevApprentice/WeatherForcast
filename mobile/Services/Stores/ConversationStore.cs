using mobile.Models;
using mobile.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace mobile
{
    /// <summary>
    /// Implémentation du store de conversations
    /// </summary>
    public class ConversationStore : IConversationStore
    {
        private readonly ObservableCollection<Conversation> _conversations = new();
        private Conversation? _supportConversation;
        private string _currentUserId = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

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
        /// Initialise le store avec la conversation de support
        /// </summary>
        public void Initialize(string currentUserId, string currentUserDisplayName)
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
        public void AddOrUpdateConversation(Conversation conversation)
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
        public void AddMessageToConversation(string conversationId, Message message)
        {
            var conversation = GetConversation(conversationId);
            if (conversation != null)
            {
                conversation.Messages.Add(message);
                SortConversations();
                OnPropertyChanged(nameof(TotalUnreadCount));
            }
        }

        /// <summary>
        /// Marque tous les messages d'une conversation comme lus
        /// </summary>
        public void MarkConversationAsRead(string conversationId)
        {
            var conversation = GetConversation(conversationId);
            if (conversation != null)
            {
                foreach (var message in conversation.Messages.Where(m => !m.IsRead))
                {
                    message.IsRead = true;
                }
                OnPropertyChanged(nameof(TotalUnreadCount));
            }
        }

        /// <summary>
        /// Marque toutes les conversations comme lues
        /// </summary>
        public void MarkAllAsRead()
        {
            foreach (var conversation in _conversations)
            {
                foreach (var message in conversation.Messages.Where(m => !m.IsRead))
                {
                    message.IsRead = true;
                }
            }
            OnPropertyChanged(nameof(TotalUnreadCount));
        }

        /// <summary>
        /// Supprime une conversation (sauf support)
        /// </summary>
        public void RemoveConversation(string conversationId)
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
        public Conversation? GetConversation(string conversationId)
        {
            return _conversations.FirstOrDefault(c => c.Id == conversationId);
        }

        /// <summary>
        /// Crée une nouvelle conversation directe
        /// </summary>
        public Conversation CreateDirectConversation(ConversationMember otherMember, string currentUserId)
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
        /// Efface toutes les conversations sauf support
        /// </summary>
        public void ClearAll()
        {
            var conversationsToRemove = _conversations
                .Where(c => c.Id != "support")
                .ToList();

            foreach (var conversation in conversationsToRemove)
            {
                _conversations.Remove(conversation);
            }

            OnPropertyChanged(nameof(TotalUnreadCount));
        }

        /// <summary>
        /// Trie les conversations : support épinglé en premier, puis par dernière activité
        /// </summary>
        private void SortConversations()
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

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
