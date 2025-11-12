using Microsoft.Maui.Controls.Shapes;
using mobile.Helpers;
using mobile.Services.Stores;

namespace mobile.Controls
{
    /// <summary>
    /// Popup de chat Support style Messenger
    /// S'affiche en overlay avec animation
    /// </summary>
    public partial class SupportChatPopup : ContentView
    {
        private readonly IConversationStore _conversationStore;
        private const string SUPPORT_CONVERSATION_ID = "support";

        // Constructeur par défaut pour le XAML designer
        public SupportChatPopup() : this(
            Application.Current?.Handler?.MauiContext?.Services.GetService<IConversationStore>()!)
        {
        }

        // Constructeur avec injection de dépendances
        public SupportChatPopup(IConversationStore conversationStore)
        {
            InitializeComponent();

            _conversationStore = conversationStore ?? throw new ArgumentNullException(nameof(conversationStore));
            
            // Adapter le layout selon l'appareil et l'orientation
            ApplyResponsiveLayout();
            
            // S'abonner aux changements d'orientation
            DeviceDisplay.MainDisplayInfoChanged += OnDisplayInfoChanged;
        }
        
        private void OnDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
        {
            // Réappliquer le layout lors d'un changement d'orientation
            MainThread.BeginInvokeOnMainThread(() => ApplyResponsiveLayout());
        }
        
        private void ApplyResponsiveLayout()
        {
#if ANDROID || IOS
            // Sur mobile : utiliser le helper pour déterminer le layout selon le type d'appareil et l'orientation
            bool useDesktopLayout = DeviceHelper.ShouldUseDesktopLayout();
            
            if (useDesktopLayout)
            {
                // Tablette en mode paysage : layout compact en bas à droite (comme desktop)
                this.HorizontalOptions = LayoutOptions.End;
                this.VerticalOptions = LayoutOptions.End;
                this.Margin = new Thickness(0, 0, 20, 20);
                
                ChatWindow.MaximumWidthRequest = 360;
                ChatWindow.MaximumHeightRequest = 500;
                ChatWindow.StrokeShape = new RoundRectangle { CornerRadius = 16 };
            }
            else
            {
                // Téléphone ou tablette portrait : plein écran
                this.HorizontalOptions = LayoutOptions.Fill;
                this.VerticalOptions = LayoutOptions.Fill;
                this.Margin = new Thickness(0);
                
                ChatWindow.MaximumWidthRequest = double.PositiveInfinity;
                ChatWindow.MaximumHeightRequest = double.PositiveInfinity;
                ChatWindow.StrokeShape = new RoundRectangle { CornerRadius = 0 };
            }
            
            // Log pour debug
            System.Diagnostics.Debug.WriteLine($"📱 {DeviceHelper.GetDeviceInfo()} - Layout: {(useDesktopLayout ? "Desktop-like" : "Mobile")}");
#else
            // Sur Desktop (Windows/MacCatalyst) : ne rien faire, le XAML gère tout avec OnPlatform
            System.Diagnostics.Debug.WriteLine($"🖥️ Desktop platform - Layout handled by XAML OnPlatform");
#endif
        }

        /// <summary>
        /// Affiche le chat avec animation
        /// </summary>
        public async Task ShowAsync ()
        {
            // S'abonner aux changements
            _conversationStore.Conversations.CollectionChanged += OnConversationsChanged;
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
            var supportConversation = _conversationStore.GetConversation(SUPPORT_CONVERSATION_ID);
            if (supportConversation == null) return;

            // Mettre à jour le titre et la description
            ConversationTitleLabel.Text = supportConversation.Title ?? "Support";
        }

        /// <summary>
        /// Marque tous les messages de la conversation Support comme lus
        /// </summary>
        private async void MarkSupportMessagesAsRead ()
        {
            await _conversationStore.MarkConversationAsReadAsync(SUPPORT_CONVERSATION_ID);
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

        private void OnMessageTextChanged(object? sender, TextChangedEventArgs e)
        {
            // Activer/désactiver le bouton d'envoi selon si le champ contient du texte
            SendButton.IsEnabled = !string.IsNullOrWhiteSpace(e.NewTextValue);
        }

        private async void OnSendMessage (object? sender, EventArgs e)
        {
            var messageText = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(messageText)) return;

            // Vider le champ immédiatement
            MessageEntry.Text = string.Empty;

            // Marquer tous les messages précédents comme lus (l'utilisateur a forcément lu pour envoyer un message)
            await _conversationStore.MarkConversationAsReadAsync(SUPPORT_CONVERSATION_ID);

            // Créer le message
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                Content = messageText,
                Type = MessageType.User,
                Timestamp = DateTime.Now,
                IsRead = true
            };

            // Envoyer via le store
            var success = await _conversationStore.SendMessageAsync(SUPPORT_CONVERSATION_ID, message);

            if (success)
            {
                // Scroll vers le bas après un court délai pour laisser le temps au rechargement
                await Task.Delay(150);
                await MessagesScrollView.ScrollToAsync(0, MessagesList.Height, true);
            }
            else
            {
                // En cas d'échec, restaurer le texte
                MessageEntry.Text = messageText;
                System.Diagnostics.Debug.WriteLine("❌ Échec de l'envoi du message");
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
                    IsRead = false
                };

                // Ajouter directement au store (pas besoin d'envoyer via le service pour les FAQ)
                _conversationStore.AddMessageToConversation(SUPPORT_CONVERSATION_ID, supportMessage);

                // Marquer immédiatement comme lu car le popup est ouvert
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    System.Diagnostics.Debug.WriteLine("💬 FAQ answer added to conversation");

                    // Petit délai pour laisser le message s'ajouter
                    await Task.Delay(50);

                    // Marquer comme lu via le store
                    await _conversationStore.MarkConversationAsReadAsync(SUPPORT_CONVERSATION_ID);

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
