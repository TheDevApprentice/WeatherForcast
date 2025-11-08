using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace mobile.Controls
{
    /// <summary>
    /// Centre de Message affichant toutes les messages reçues
    /// </summary>
    public partial class MessageCenterView : ContentView, INotifyPropertyChanged
    {
        private readonly IMessageStore _messageStore;
        private bool _hasUnreadMessages;

        public new event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Collection de notifications à afficher
        /// </summary>
        public ObservableCollection<Message> Messages => _messageStore.Messages;

        /// <summary>
        /// Indique s'il y a des notifications non lues
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
            // Récupérer le store depuis le service provider
            _messageStore = Application.Current?.Handler?.MauiContext?.Services.GetService<IMessageStore>()
                ?? throw new InvalidOperationException("IMessageStore not found");

            InitializeComponent();

            // S'abonner aux changements du store
            _messageStore.PropertyChanged += OnStorePropertyChanged;
            UpdateHasUnreadMessages();
        }

        private void OnStorePropertyChanged (object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMessageStore.UnreadCount))
            {
                UpdateHasUnreadMessages();
            }
        }

        private void UpdateHasUnreadMessages ()
        {
            _hasUnreadMessages = _messageStore.UnreadCount > 0;
        }

        private void OnMarkAllAsReadClicked (object sender, EventArgs e)
        {
            _messageStore.MarkAllAsRead();
        }

        private void OnDeleteMessageClicked (object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string messageId)
            {
                _messageStore.RemoveMessage(messageId);
            }
        }

        private void OnClearAllClicked (object sender, EventArgs e)
        {
            _messageStore.ClearAll();
        }

        private async void OnCloseClicked (object sender, EventArgs e)
        {
            // Fermer la page modale parente
            var parentPage = this.GetParentPage();
            if (parentPage != null && parentPage.Navigation != null)
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
