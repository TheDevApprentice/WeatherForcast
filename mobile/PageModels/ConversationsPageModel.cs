using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Services.Stores;
using System.Collections.ObjectModel;

namespace mobile.PageModels
{
    /// <summary>
    /// PageModel pour la page des conversations
    /// Gère l'affichage et la recherche des conversations
    /// </summary>
    public partial class ConversationsPageModel : ObservableObject
    {
        private readonly IConversationStore _conversationStore;

        [ObservableProperty]
        private ObservableCollection<Conversation> _conversations = new();

        [ObservableProperty]
        private ObservableCollection<Conversation> _filteredConversations = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isEmpty = false;

        [ObservableProperty]
        private bool _isRefreshing = false;

        public ConversationsPageModel(IConversationStore conversationStore)
        {
            _conversationStore = conversationStore;

            // S'abonner aux changements du store
            _conversationStore.Conversations.CollectionChanged += OnConversationsChanged;
        }

        /// <summary>
        /// Initialise le PageModel
        /// </summary>
        public async Task InitializeAsync()
        {
            await LoadConversationsAsync();
        }

        /// <summary>
        /// Charge les conversations
        /// </summary>
        [RelayCommand]
        private async Task LoadConversationsAsync()
        {
            try
            {
                IsRefreshing = true;

                // Mettre à jour la liste locale depuis le store
                Conversations = new ObservableCollection<Conversation>(_conversationStore.Conversations);
                ApplyFilter();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ConversationsPageModel", $"❌ Error loading conversations: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// Gère les changements du store
        /// </summary>
        private void OnConversationsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Conversations = new ObservableCollection<Conversation>(_conversationStore.Conversations);
                ApplyFilter();
            });
        }

        /// <summary>
        /// Applique le filtre de recherche
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        /// <summary>
        /// Applique le filtre de recherche sur les conversations
        /// </summary>
        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredConversations = new ObservableCollection<Conversation>(Conversations);
            }
            else
            {
                var currentUserId = _conversationStore.GetCurrentUserId();
                var searchLower = SearchText.ToLower();
                var filtered = Conversations.Where(c =>
                    c.GetDisplayName(currentUserId).ToLower().Contains(searchLower) ||
                    (c.LastMessage?.Content.ToLower().Contains(searchLower) ?? false)
                ).ToList();

                FilteredConversations = new ObservableCollection<Conversation>(filtered);
            }

            IsEmpty = FilteredConversations.Count == 0;
        }

        /// <summary>
        /// Navigue vers une conversation
        /// </summary>
        [RelayCommand]
        private async Task NavigateToConversationAsync(Conversation conversation)
        {
            if (conversation == null)
                return;

            await Shell.Current.GoToAsync($"conversation-detail?conversationId={conversation.Id}");
        }

        /// <summary>
        /// Crée une nouvelle conversation
        /// </summary>
        [RelayCommand]
        private async Task CreateNewConversationAsync()
        {
            // TODO: Implémenter la création d'une nouvelle conversation
            await Shell.Current.DisplayAlert("Nouvelle conversation", "Fonctionnalité à venir", "OK");
        }

        /// <summary>
        /// Rafraîchit les conversations
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadConversationsAsync();
        }
    }
}
