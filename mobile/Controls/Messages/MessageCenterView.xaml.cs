using System.ComponentModel;
using System.Runtime.CompilerServices;
using mobile.Models;

namespace mobile.Controls
{
    /// <summary>
    /// Centre de conversations affichant toutes les conversations de l'utilisateur
    /// </summary>
    public partial class MessageCenterView : ContentView, INotifyPropertyChanged
    {
        private readonly IConversationStore _conversationStore;
        private bool _hasUnreadMessages;
        private string _currentUserId = "current-user"; // TODO: Récupérer l'ID utilisateur réel

        public new event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Indique s'il y a des messages non lus
        /// </summary>
        public bool HasUnreadMessages
        {
            get => _hasUnreadMessages;
            private set
            {
                if (_hasUnreadMessages != value)
                {
                    _hasUnreadMessages = value;
                    OnPropertyChanged();
                }
            }
        }

        public MessageCenterView ()
        {
            // Récupérer les services
            _conversationStore = Application.Current?.Handler?.MauiContext?.Services.GetService<IConversationStore>()
                ?? throw new InvalidOperationException("IConversationStore not found");

            InitializeComponent();

            // S'abonner aux changements du store
            _conversationStore.PropertyChanged += OnStorePropertyChanged;
            _conversationStore.Conversations.CollectionChanged += OnConversationsChanged;
            
            // Initialiser l'affichage
            UpdateConversations();
            UpdateHasUnreadMessages();
        }

        private void OnStorePropertyChanged (object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IConversationStore.TotalUnreadCount))
            {
                UpdateHasUnreadMessages();
            }
        }

        private void OnConversationsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateConversations();
        }

        private void UpdateConversations()
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Vider la liste actuelle
                ConversationsList.Children.Clear();

                // Afficher l'empty view si aucune conversation
                EmptyView.IsVisible = _conversationStore.Conversations.Count == 0;

                // Ajouter les cartes de conversation
                foreach (var conversation in _conversationStore.Conversations)
                {
                    var card = new ConversationCard();
                    card.Initialize(conversation, _currentUserId);
                    ConversationsList.Children.Add(card);
                }
            });
        }

        private void UpdateHasUnreadMessages ()
        {
            _hasUnreadMessages = _conversationStore.TotalUnreadCount > 0;
        }

        private void OnMarkAllAsReadClicked (object sender, EventArgs e)
        {
            _conversationStore.MarkAllAsRead();
        }

        private void OnClearAllClicked (object sender, EventArgs e)
        {
            _conversationStore.ClearAll();
        }

        private async void OnViewAllClicked(object sender, EventArgs e)
        {
            // Fermer le modal
            var parentPage = GetParentPage();
            if (parentPage?.Navigation != null)
            {
                await parentPage.Navigation.PopModalAsync(animated: false);
            }

            // Naviguer vers la page des conversations
            if (Application.Current?.MainPage is Shell shell)
            {
                await shell.GoToAsync("///conversations");
            }
        }

        private async void OnCloseClicked (object sender, EventArgs e)
        {
            // Fermer la page modale parente
            var parentPage = GetParentPage();
            if (parentPage?.Navigation != null)
            {
                await parentPage.Navigation.PopModalAsync();
            }
        }

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

        protected new void OnPropertyChanged ([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
