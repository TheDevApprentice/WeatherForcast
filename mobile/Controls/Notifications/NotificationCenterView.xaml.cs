using mobile.Services.Stores;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace mobile.Controls
{
    /// <summary>
    /// Centre de notifications affichant toutes les notifications reçues
    /// </summary>
    public partial class NotificationCenterView : ContentView, INotifyPropertyChanged
    {
        private readonly INotificationStore _notificationStore;
        private bool _hasUnreadNotifications;

        public new event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Collection de notifications à afficher
        /// </summary>
        public ObservableCollection<Notification> Notifications => _notificationStore.Notifications;

        /// <summary>
        /// Indique s'il y a des notifications non lues
        /// </summary>
        public bool HasUnreadNotifications
        {
            get => _hasUnreadNotifications;
            private set
            {
                if (_hasUnreadNotifications != value)
                {
                    _hasUnreadNotifications = value;
                    OnPropertyChanged();
                }
            }
        }

        public NotificationCenterView ()
        {
            // Récupérer le store depuis le service provider
            _notificationStore = Application.Current?.Handler?.MauiContext?.Services.GetService<INotificationStore>()
                ?? throw new InvalidOperationException("INotificationStore not found");

            InitializeComponent();

            // S'abonner aux changements du store
            _notificationStore.PropertyChanged += OnStorePropertyChanged;
            UpdateHasUnreadNotifications();
        }

        private void OnStorePropertyChanged (object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(INotificationStore.UnreadCount))
            {
                UpdateHasUnreadNotifications();
            }
        }

        private void UpdateHasUnreadNotifications ()
        {
            HasUnreadNotifications = _notificationStore.UnreadCount > 0;
        }

        private void OnMarkAllAsReadClicked (object sender, EventArgs e)
        {
            _notificationStore.MarkAllAsRead();
        }

        private void OnDeleteNotificationClicked (object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string notificationId)
            {
                _notificationStore.RemoveNotification(notificationId);
            }
        }

        private void OnClearAllClicked (object sender, EventArgs e)
        {
            _notificationStore.ClearAll();
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
