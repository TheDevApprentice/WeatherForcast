using Microsoft.Maui.Controls.Shapes;
using mobile.Controls;
using System.Windows.Input;

namespace mobile.Pages
{
    /// <summary>
    /// Page affichant les messages d'une conversation et permettant d'envoyer des messages
    /// </summary>
    [QueryProperty(nameof(ConversationId), "conversationId")]
    public partial class ConversationDetailPage : ContentPage
    {
        private readonly IConversationStore _conversationStore;
        private string _conversationId = string.Empty;
        private Conversation? _conversation;
        private string _currentUserId = "current-user"; // TODO: Récupérer l'ID utilisateur réel

        public ICommand SendMessageCommand { get; }

        public string ConversationId
        {
            get => _conversationId;
            set
            {
                _conversationId = value;
                LoadConversation();
            }
        }

        public ConversationDetailPage ()
        {
            InitializeComponent();

            // Récupérer le store
            _conversationStore = Application.Current?.Handler?.MauiContext?.Services.GetService<IConversationStore>()
                ?? throw new InvalidOperationException("IConversationStore not found");

            // Commande pour envoyer un message
            SendMessageCommand = new Command(OnSendMessage);

            // S'abonner aux changements du texte
            MessageEntry.TextChanged += OnMessageTextChanged;
        }

        private void LoadConversation ()
        {
            if (string.IsNullOrEmpty(_conversationId))
                return;

            _conversation = _conversationStore.GetConversation(_conversationId);

            if (_conversation == null)
            {
                DisplayAlert("Erreur", "Conversation introuvable", "OK");
                Shell.Current.GoToAsync("..");
                return;
            }

            // Mettre à jour l'interface
            UpdateHeader();
            UpdateMessages();

            // Marquer la conversation comme lue
            _conversationStore.MarkConversationAsRead(_conversationId);
        }

        private void UpdateHeader ()
        {
            if (_conversation == null) return;

            ConversationTitleLabel.Text = _conversation.GetDisplayName(_currentUserId);

            // Sous-titre selon le type
            if (_conversation.Type == ConversationType.Support)
            {
                ConversationSubtitleLabel.Text = "Équipe Support";
                PinnedBadge.IsVisible = true;
            }
            else if (_conversation.Type == ConversationType.Direct)
            {
                var otherMember = _conversation.Members.FirstOrDefault(m => m.UserId != _currentUserId);
                ConversationSubtitleLabel.Text = otherMember?.DisplayName ?? "";
            }
            else
            {
                ConversationSubtitleLabel.Text = $"{_conversation.Members.Count} membres";
            }

            Title = _conversation.GetDisplayName(_currentUserId);
        }

        private void UpdateMessages ()
        {
            if (_conversation == null) return;

            // Vider la liste
            MessagesList.Children.Clear();

            // Afficher l'empty view si aucun message
            EmptyView.IsVisible = _conversation.Messages.Count == 0;

            // Ajouter les messages
            foreach (var message in _conversation.Messages.OrderBy(m => m.Timestamp))
            {
                var messageView = CreateMessageView(message);
                MessagesList.Children.Add(messageView);
            }

            // Scroller vers le bas
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                await MessagesScrollView.ScrollToAsync(0, MessagesList.Height, false);
            });
        }

        private View CreateMessageView (Message message)
        {
            // Déterminer si c'est un message de l'utilisateur ou du support
            bool isFromCurrentUser = message.Type != MessageType.Info &&
                                     message.Type != MessageType.Support;

            var border = new Border
            {
                BackgroundColor = isFromCurrentUser
                    ? Color.FromArgb("#3B82F6")
                    : Application.Current?.Resources["CardBackgroundColor"] as Color,
                Padding = new Thickness(12, 8),
                Margin = new Thickness(
                    isFromCurrentUser ? 60 : 0,
                    0,
                    isFromCurrentUser ? 0 : 60,
                    0
                ),
                StrokeThickness = isFromCurrentUser ? 0 : 1,
                Stroke = isFromCurrentUser ? null : Application.Current?.Resources["BorderColor"] as Brush,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(
                        isFromCurrentUser ? 16 : 4,
                        isFromCurrentUser ? 4 : 16,
                        16,
                        16
                    )
                },
                HorizontalOptions = isFromCurrentUser ? LayoutOptions.End : LayoutOptions.Start
            };

            var stackLayout = new VerticalStackLayout
            {
                Spacing = 4
            };

            // Titre si présent
            if (!string.IsNullOrEmpty(message.Title) && message.Title != "Message")
            {
                stackLayout.Children.Add(new Label
                {
                    Text = message.Title,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Application.Current?.Resources["PrimaryTextColor"] as Color
                });
            }

            // Contenu du message
            stackLayout.Children.Add(new Label
            {
                Text = message.Content,
                FontSize = 14,
                TextColor = Application.Current?.Resources["PrimaryTextColor"] as Color,
                LineBreakMode = LineBreakMode.WordWrap
            });

            // Timestamp
            stackLayout.Children.Add(new Label
            {
                Text = FormatTimestamp(message.Timestamp),
                FontSize = 11,
                TextColor = Application.Current?.Resources["TertiaryTextColor"] as Color,
                Opacity = 0.8,
                HorizontalOptions = LayoutOptions.End
            });

            border.Content = stackLayout;
            return border;
        }

        private string FormatTimestamp (DateTime timestamp)
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

        private void OnMessageTextChanged (object? sender, TextChangedEventArgs e)
        {
            // Activer/désactiver le bouton envoyer selon si le texte est vide
            SendButton.IsEnabled = !string.IsNullOrWhiteSpace(e.NewTextValue);
        }

        private void OnSendMessage ()
        {
            var messageText = MessageEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(messageText) || _conversation == null)
                return;

            // Créer le nouveau message
            var newMessage = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Message",
                Content = messageText,
                Type = MessageType.User,
                Timestamp = DateTime.Now,
                IsRead = true,
                WasDisplayed = true
            };

            // Ajouter le message à la conversation
            _conversationStore.AddMessageToConversation(_conversationId, newMessage);

            // Vider le champ de saisie
            MessageEntry.Text = string.Empty;

            // Mettre à jour l'affichage
            UpdateMessages();
        }

        private void OnSendMessageClicked (object sender, EventArgs e)
        {
            OnSendMessage();
        }

        private async void OnBackClicked (object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
