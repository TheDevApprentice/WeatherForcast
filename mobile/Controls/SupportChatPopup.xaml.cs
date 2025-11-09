using Microsoft.Maui.Controls.Shapes;

namespace mobile.Controls
{
    /// <summary>
    /// Popup de chat Support style Messenger
    /// S'affiche en overlay avec animation
    /// </summary>
    public partial class SupportChatPopup : ContentView
    {
        private readonly IConversationStore? _conversationStore;
        private const string SUPPORT_CONVERSATION_ID = "support";
        private string _currentUserId = "current-user";

        public SupportChatPopup ()
        {
            InitializeComponent();

            // Récupérer le ConversationStore
            _conversationStore = Application.Current?.Handler?.MauiContext?.Services.GetService<IConversationStore>();

            if (_conversationStore != null)
            {
                // S'abonner aux changements
                _conversationStore.Conversations.CollectionChanged += OnConversationsChanged;
            }
        }

        /// <summary>
        /// Affiche le chat avec animation
        /// </summary>
        public async Task ShowAsync ()
        {
            // Charger les infos de la conversation Support
            LoadConversationInfo();

            // Charger les messages
            LoadMessages();

            // Marquer tous les messages de la conversation Support comme lus
            MarkSupportMessagesAsRead();

            // Cacher le bouton Support
            var supportButton = FindSupportButton();
            if (supportButton != null)
            {
                supportButton.IsVisible = false;
            }

            // Rendre visible
            this.IsVisible = true;

            // Animation d'apparition
            await Task.WhenAll(
                this.FadeTo(1, 250, Easing.SinOut),
                ChatWindow.TranslateTo(0, 0, 300, Easing.SinOut)
            );

            // Focus sur le champ de saisie
            MessageEntry.Focus();
        }

        private void LoadConversationInfo ()
        {
            if (_conversationStore == null) return;

            var supportConversation = _conversationStore.GetConversation(SUPPORT_CONVERSATION_ID);
            if (supportConversation == null) return;

            // Mettre à jour le titre et la description
            ConversationTitleLabel.Text = supportConversation.Title ?? "Support";
        }

        /// <summary>
        /// Marque tous les messages de la conversation Support comme lus
        /// </summary>
        private void MarkSupportMessagesAsRead ()
        {
            if (_conversationStore == null) return;

            var supportConversation = _conversationStore.GetConversation(SUPPORT_CONVERSATION_ID);
            if (supportConversation == null) return;

            // Marquer tous les messages non lus comme lus
            var hasChanges = false;
            foreach (var message in supportConversation.Messages.Where(m => !m.IsRead))
            {
                message.IsRead = true;
                hasChanges = true;
            }

            // Notifier les changements si nécessaire
            if (hasChanges)
            {
                supportConversation.NotifyPropertyChanged(nameof(supportConversation.Messages));
                System.Diagnostics.Debug.WriteLine($"✅ Messages Support marqués comme lus");
            }
        }

        /// <summary>
        /// Masque le chat avec animation
        /// </summary>
        public async Task HideAsync ()
        {
            await Task.WhenAll(
                this.FadeTo(0, 200, Easing.SinIn),
                ChatWindow.TranslateTo(0, 600, 250, Easing.SinIn)
            );

            this.IsVisible = false;

            // Réafficher le bouton Support
            var supportButton = FindSupportButton();
            if (supportButton != null)
            {
                supportButton.IsVisible = true;
            }
        }

        private SupportButton? FindSupportButton ()
        {
            // Chercher le bouton Support dans le parent
            var parent = this.Parent;
            while (parent != null)
            {
                if (parent is Layout layout)
                {
                    foreach (var child in layout.Children)
                    {
                        if (child is SupportButton button)
                            return button;
                    }
                }
                parent = parent.Parent;
            }
            return null;
        }

        private void LoadMessages ()
        {
            MessagesList.Children.Clear();

            if (_conversationStore == null) return;

            var supportConversation = _conversationStore.GetConversation(SUPPORT_CONVERSATION_ID);
            if (supportConversation == null) return;

            foreach (var message in supportConversation.Messages)
            {
                AddMessageBubble(message);
            }

            // Scroll vers le bas
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                await MessagesScrollView.ScrollToAsync(0, MessagesList.Height, false);
            });
        }

        private void AddMessageBubble (Message message)
        {
            bool isFromUser = message.Type == MessageType.User;

            var bubble = new Border
            {
                BackgroundColor = isFromUser
                    ? Color.FromArgb("#3B82F6")
                    : Color.FromArgb("#E5E7EB"),
                Padding = new Thickness(12, 8),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                HorizontalOptions = isFromUser ? LayoutOptions.End : LayoutOptions.Start,
                MaximumWidthRequest = 260
            };

            var label = new Label
            {
                Text = message.Content,
                TextColor = Application.Current?.Resources["PrimaryTextColor"] as Color,
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap
            };

            bubble.Content = label;
            MessagesList.Children.Add(bubble);
        }

        private void OnConversationsChanged (object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Recharger les messages si la conversation Support change
            MainThread.BeginInvokeOnMainThread(() => LoadMessages());
        }

        private async void OnSendMessage (object? sender, EventArgs e)
        {
            var messageText = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(messageText)) return;

            // Vider le champ immédiatement
            MessageEntry.Text = string.Empty;

            // Marquer tous les messages précédents comme lus (l'utilisateur a forcément lu pour envoyer un message)
            MarkSupportMessagesAsRead();

            // Créer le message
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Content = messageText,
                Type = MessageType.User,
                Timestamp = DateTime.Now,
                IsRead = true
            };

            // Ajouter à la conversation (OnConversationsChanged va recharger automatiquement)
            _conversationStore?.AddMessageToConversation(SUPPORT_CONVERSATION_ID, message);

            // Scroll vers le bas après un court délai pour laisser le temps au rechargement
            await Task.Delay(150);
            await MessagesScrollView.ScrollToAsync(0, MessagesList.Height, true);

            // Simuler une réponse du support après 2 secondes
            SimulateSupportResponse();
        }

        private async void SimulateSupportResponse ()
        {
            await Task.Delay(2000);

            var responses = new[]
            {
                "Merci pour votre message ! Un membre de notre équipe vous répondra dans les plus brefs délais.",
                "Nous avons bien reçu votre demande. Comment pouvons-nous vous aider ?",
                "Bonjour ! Je suis là pour vous aider. Pouvez-vous me donner plus de détails ?"
            };

            var random = new Random();
            var responseText = responses[random.Next(responses.Length)];

            var supportMessage = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Content = responseText,
                Type = MessageType.Support,
                Timestamp = DateTime.Now,
                IsRead = false // Sera marqué comme lu juste après
            };

            _conversationStore?.AddMessageToConversation(SUPPORT_CONVERSATION_ID, supportMessage);

            // Si le popup est toujours visible, marquer comme lu
            if (this.IsVisible)
            {
                await Task.Delay(50);
                MarkSupportMessagesAsRead();
                System.Diagnostics.Debug.WriteLine("✅ Réponse simulée marquée comme lue (popup ouvert)");
            }
        }

        private async void OnCloseClicked (object? sender, EventArgs e)
        {
            await HideAsync();
        }

        private void OnToggleFaqClicked (object? sender, EventArgs e)
        {
            // Toggle la visibilité de la liste FAQ
            FaqList.IsVisible = !FaqList.IsVisible;

            // Changer l'icône (▼ quand ouvert, ▲ quand fermé)
            FaqToggleIcon.Text = FaqList.IsVisible ? "▲" : "▼";
        }

        private void OnFaqTapped (object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🔍 FAQ tapped - sender type: " + sender?.GetType().Name);

            string? question = null;

            // Essayer de récupérer le CommandParameter depuis le TapGestureRecognizer
            if (e is TappedEventArgs tappedArgs && tappedArgs.Parameter is string param)
            {
                question = param;
                System.Diagnostics.Debug.WriteLine($"✅ Question from TappedEventArgs: {question}");
            }
            else if (sender is BindableObject bindable)
            {
                // Chercher dans les GestureRecognizers
                if (bindable is View view && view.GestureRecognizers.Count > 0)
                {
                    foreach (var recognizer in view.GestureRecognizers)
                    {
                        if (recognizer is TapGestureRecognizer tapRecognizer)
                        {
                            question = tapRecognizer.CommandParameter as string;
                            System.Diagnostics.Debug.WriteLine($"✅ Question from TapGestureRecognizer: {question}");
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(question))
            {
                System.Diagnostics.Debug.WriteLine("❌ Question is null or empty");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"📝 Processing FAQ: {question}");

            // Réponses automatiques pour chaque FAQ
            var responses = new Dictionary<string, string>
            {
                ["Comment réinitialiser mon mot de passe ?"] =
                    "Pour réinitialiser votre mot de passe :\n\n1. Allez dans Paramètres > Compte\n2. Cliquez sur 'Modifier le mot de passe'\n3. Suivez les instructions à l'écran\n\nVous recevrez un email de confirmation.",

                ["Comment mettre à jour mes informations ?"] =
                    "Pour mettre à jour vos informations :\n\n1. Accédez à votre Profil\n2. Cliquez sur 'Modifier'\n3. Modifiez les champs souhaités\n4. Enregistrez les modifications\n\nVos changements seront synchronisés automatiquement.",

                ["Comment contacter le support technique ?"] =
                    "Vous pouvez nous contacter de plusieurs façons :\n\n📧 Email : support@weatherforecast.com\n📞 Téléphone : +33 1 23 45 67 89\n💬 Chat : Directement ici !\n\nNous répondons sous 24h maximum."
            };

            if (responses.TryGetValue(question, out var answer))
            {
                System.Diagnostics.Debug.WriteLine($"✅ Found answer for: {question}");

                // Créer un message automatique du support
                var supportMessage = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = answer,
                    Type = MessageType.Info,
                    Timestamp = DateTime.Now,
                    IsRead = false // Sera marqué comme lu juste après
                };

                // Ajouter à la conversation (OnConversationsChanged va recharger automatiquement)
                _conversationStore?.AddMessageToConversation(SUPPORT_CONVERSATION_ID, supportMessage);

                // Marquer immédiatement comme lu car le popup est ouvert
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    System.Diagnostics.Debug.WriteLine("💬 FAQ answer added to conversation");
                    
                    // Petit délai pour laisser le message s'ajouter
                    await Task.Delay(50);
                    
                    // Marquer comme lu car l'utilisateur voit le message dans le popup ouvert
                    MarkSupportMessagesAsRead();
                    
                    // Scroll vers le bas
                    await Task.Delay(100);
                    await MessagesScrollView.ScrollToAsync(0, MessagesList.Height, true);
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ No answer found for: {question}");
            }
        }
    }
}
