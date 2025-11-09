using mobile.Controls;

namespace mobile.Pages
{
    /// <summary>
    /// Page affichant toutes les conversations de l'utilisateur
    /// </summary>
    public partial class ConversationsPage : ContentPage
    {
        private readonly IConversationStore _conversationStore;
        private string _currentUserId = "current-user"; // TODO: Récupérer l'ID utilisateur réel
        private List<Conversation> _allConversations = new();

        public ConversationsPage ()
        {
            InitializeComponent();

            // Récupérer le store
            _conversationStore = Application.Current?.Handler?.MauiContext?.Services.GetService<IConversationStore>()
                ?? throw new InvalidOperationException("IConversationStore not found");

            // S'abonner aux changements
            _conversationStore.Conversations.CollectionChanged += OnConversationsChanged;

            // Charger les conversations
            LoadConversations();
        }

        private void OnConversationsChanged (object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            LoadConversations();
        }

        private void LoadConversations ()
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                _allConversations = _conversationStore.Conversations.ToList();
                UpdateConversationsDisplay(_allConversations);
            });
        }

        private void UpdateConversationsDisplay (List<Conversation> conversations)
        {
            // Vider la liste
            ConversationsList.Children.Clear();

            // Afficher l'empty view si aucune conversation
            EmptyView.IsVisible = conversations.Count == 0;

            // Ajouter les cartes de conversation
            foreach (var conversation in conversations)
            {
                var card = new ConversationCard();
                card.Initialize(conversation, _currentUserId);
                ConversationsList.Children.Add(card);
            }
        }


        private void OnSearchTextChanged (object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                UpdateConversationsDisplay(_allConversations);
                return;
            }

            var filtered = _allConversations.Where(c =>
                c.GetDisplayName(_currentUserId).ToLower().Contains(searchText) ||
                (c.LastMessage?.Content.ToLower().Contains(searchText) ?? false)
            ).ToList();

            UpdateConversationsDisplay(filtered);
        }

        private async void OnNewConversationClicked (object sender, EventArgs e)
        {
            // TODO: Implémenter la création d'une nouvelle conversation
            await DisplayAlert("Nouvelle conversation", "Fonctionnalité à venir", "OK");
        }
    }
}
