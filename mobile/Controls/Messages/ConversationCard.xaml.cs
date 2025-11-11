namespace mobile.Controls
{
    /// <summary>
    /// Carte représentant une conversation dans le centre de messages
    /// </summary>
    public partial class ConversationCard : Border
    {
        private Conversation? _conversation;
        private string _currentUserId = string.Empty;

        public ConversationCard ()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialise la carte avec une conversation
        /// </summary>
        public void Initialize (Conversation conversation, string currentUserId)
        {
            _conversation = conversation;
            _currentUserId = currentUserId;
            BindingContext = conversation;

            UpdateUI();
        }

        /// <summary>
        /// Met à jour l'interface utilisateur
        /// </summary>
        private void UpdateUI ()
        {
            if (_conversation == null) return;

            // Initiales
            InitialsLabel.Text = _conversation.GetInitials(_currentUserId);

            // Nom d'affichage
            DisplayNameLabel.Text = _conversation.GetDisplayName(_currentUserId);

            // Dernier message
            if (_conversation.LastMessage != null)
            {
                LastMessageLabel.Text = _conversation.LastMessage.Content;
                TimestampLabel.Text = FormatTimestamp(_conversation.LastMessage.Timestamp);
            }
            else
            {
                LastMessageLabel.Text = "Aucun message";
                TimestampLabel.Text = "";
            }

            // Compteur de messages non lus
            if (_conversation.HasUnreadMessages)
            {
                var count = _conversation.UnreadCount;
                UnreadCountLabel.Text = count > 99 ? "99+" : count.ToString();
            }
        }

        /// <summary>
        /// Formate le timestamp de manière relative
        /// </summary>
        private string FormatTimestamp (DateTime timestamp)
        {
            var now = DateTime.Now;
            var diff = now - timestamp;

            if (diff.TotalMinutes < 1)
                return "À l'instant";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}h";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}j";

            return timestamp.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Appelé quand on clique sur la conversation
        /// </summary>
        private async void OnConversationTapped (object? sender, EventArgs e)
        {
            if (_conversation == null) return;

            // Détecter si on est dans un modal
            var parentPage = GetParentPage();
            bool isInModal = parentPage?.Navigation?.ModalStack.Count > 0;

            // Si on est dans un modal, le fermer d'abord
            if (isInModal && parentPage?.Navigation != null)
            {
                await parentPage.Navigation.PopModalAsync(animated: false);
            }

            // Naviguer vers la page de conversation
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync($"///conversations/detail?conversationId={_conversation.Id}");
            }
        }

        /// <summary>
        /// Trouve la page parente
        /// </summary>
        private Page? GetParentPage ()
        {
            Element? parent = this.Parent;
            while (parent != null)
            {
                if (parent is Page page)
                    return page;
                parent = parent.Parent;
            }
            return null;
        }
    }
}
