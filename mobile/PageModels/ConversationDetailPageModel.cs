using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Controls;
using mobile.Services.Stores;
using System.Collections.ObjectModel;

namespace mobile.PageModels
{
    /// <summary>
    /// PageModel pour la page de détail d'une conversation
    /// Gère l'affichage des messages et l'envoi de nouveaux messages
    /// </summary>
    public partial class ConversationDetailPageModel : ObservableObject, IQueryAttributable
    {
        private readonly IConversationStore _conversationStore;

        [ObservableProperty]
        private string _conversationId = string.Empty;

        [ObservableProperty]
        private Conversation? _conversation;

        [ObservableProperty]
        private string _conversationTitle = string.Empty;

        [ObservableProperty]
        private string _conversationSubtitle = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Message> _messages = new();

        [ObservableProperty]
        private string _messageText = string.Empty;

        [ObservableProperty]
        private bool _canSend = false;

        [ObservableProperty]
        private bool _isEmpty = false;

        [ObservableProperty]
        private bool _isTyping = false;

        public ConversationDetailPageModel(IConversationStore conversationStore)
        {
            _conversationStore = conversationStore;
        }

        /// <summary>
        /// Applique les paramètres de requête
        /// </summary>
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("conversationId", out var conversationId))
            {
                ConversationId = conversationId?.ToString() ?? string.Empty;
            }
        }

        /// <summary>
        /// Initialise le PageModel
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadConversationAsync();
        }

        /// <summary>
        /// Charge la conversation
        /// </summary>
        partial void OnConversationIdChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _ = LoadConversationAsync();
            }
        }

        /// <summary>
        /// Charge la conversation et ses messages
        /// </summary>
        [RelayCommand]
        private async Task LoadConversationAsync()
        {
            if (string.IsNullOrEmpty(ConversationId))
                return;

            Conversation = _conversationStore.GetConversation(ConversationId);

            if (Conversation == null)
            {
                await Shell.Current.DisplayAlert("Erreur", "Conversation introuvable", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Mettre à jour le header
            UpdateHeader();

            // Charger les messages
            await LoadMessagesAsync();

            // Marquer la conversation comme lue
            await _conversationStore.MarkConversationAsReadAsync(ConversationId);
        }

        /// <summary>
        /// Met à jour le header de la conversation
        /// </summary>
        private void UpdateHeader()
        {
            if (Conversation == null)
                return;

            var currentUserId = _conversationStore.GetCurrentUserId();
            ConversationTitle = Conversation.GetDisplayName(currentUserId);

            // Sous-titre selon le type
            if (Conversation.Type == ConversationType.Support)
            {
                ConversationSubtitle = "Équipe Support";
            }
            else if (Conversation.Type == ConversationType.Direct)
            {
                var otherMember = Conversation.Members.FirstOrDefault(m => m.UserId != currentUserId);
                ConversationSubtitle = otherMember?.DisplayName ?? "";
            }
            else
            {
                ConversationSubtitle = $"{Conversation.Members.Count} membres";
            }
        }

        /// <summary>
        /// Charge les messages de la conversation
        /// Thread-safe: met à jour la collection sur le thread UI
        /// </summary>
        [RelayCommand]
        private async Task LoadMessagesAsync()
        {
            if (Conversation == null)
                return;

            // Mettre à jour la liste locale depuis le store
            // Ne pas remplacer la collection, mais modifier son contenu
            var orderedMessages = Conversation.Messages.OrderBy(m => m.Timestamp).ToList();
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Clear();
                foreach (var message in orderedMessages)
                {
                    Messages.Add(message);
                }

                IsEmpty = Messages.Count == 0;
            });
        }

        /// <summary>
        /// Gère le changement du texte du message
        /// </summary>
        partial void OnMessageTextChanged(string value)
        {
            CanSend = !string.IsNullOrWhiteSpace(value);

            // Envoyer un indicateur de frappe si le texte n'est pas vide
            if (!string.IsNullOrWhiteSpace(value) && !IsTyping)
            {
                IsTyping = true;
                _ = SendTypingIndicatorAsync();
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                IsTyping = false;
            }
        }

        /// <summary>
        /// Envoie un indicateur de frappe
        /// </summary>
        private async Task SendTypingIndicatorAsync()
        {
            // TODO: Envoyer l'indicateur de frappe via le store si nécessaire
            // await _conversationStore.SendTypingIndicatorAsync(ConversationId);
        }

        /// <summary>
        /// Envoie un message
        /// </summary>
        [RelayCommand]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageText) || Conversation == null)
                return;

            var messageContent = MessageText.Trim();
            MessageText = string.Empty; // Vider le champ immédiatement
            CanSend = false;
            IsTyping = false;

            // Créer le nouveau message
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Content = messageContent,
                Type = MessageType.User,
                Timestamp = DateTime.Now,
                IsRead = true,
                WasDisplayed = false
            };

            // Envoyer le message via le store
            var success = await _conversationStore.SendMessageAsync(ConversationId, message);

            if (success)
            {
                // Le message est déjà ajouté au store par le service
                // Recharger les messages pour mettre à jour l'affichage
                await LoadMessagesAsync();
            }
            else
            {
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'envoyer le message", "OK");
                // Restaurer le texte en cas d'échec
                MessageText = messageContent;
            }
        }


        /// <summary>
        /// Retourne le format d'affichage du timestamp
        /// </summary>
        public static string FormatTimestamp(DateTime timestamp)
        {
            var now = DateTime.Now;
            var diff = now - timestamp;

            if (diff.TotalMinutes < 1)
                return "À l'instant";
            if (diff.TotalMinutes < 60)
                return $"Il y a {(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24)
                return timestamp.ToString("HH:mm");
            if (diff.TotalDays < 7)
                return timestamp.ToString("ddd HH:mm");

            return timestamp.ToString("dd/MM/yyyy HH:mm");
        }
    }
}
