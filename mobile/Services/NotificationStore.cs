using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using mobile.Models;

namespace mobile.Services
{
    /// <summary>
    /// Store centralisé pour gérer toutes les notifications de l'application
    /// </summary>
    public interface INotificationStore : INotifyPropertyChanged
    {
        /// <summary>
        /// Collection observable de toutes les notifications
        /// </summary>
        ObservableCollection<Notification> Notifications { get; }

        /// <summary>
        /// Nombre de notifications non lues
        /// </summary>
        int UnreadCount { get; }

        /// <summary>
        /// Ajoute une nouvelle notification au store
        /// </summary>
        void AddNotification(Notification notification);

        /// <summary>
        /// Marque une notification comme lue
        /// </summary>
        void MarkAsRead(string notificationId);

        /// <summary>
        /// Marque toutes les notifications comme lues
        /// </summary>
        void MarkAllAsRead();

        /// <summary>
        /// Supprime une notification
        /// </summary>
        void RemoveNotification(string notificationId);

        /// <summary>
        /// Supprime toutes les notifications
        /// </summary>
        void ClearAll();
    }

    /// <summary>
    /// Implémentation du store de notifications
    /// </summary>
    public class NotificationStore : INotificationStore
    {
        private readonly ObservableCollection<Notification> _notifications = new();
        private int _unreadCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Notification> Notifications => _notifications;

        public int UnreadCount
        {
            get => _unreadCount;
            private set
            {
                if (_unreadCount != value)
                {
                    _unreadCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public NotificationStore()
        {
            // S'abonner aux changements de la collection pour mettre à jour le compteur
            _notifications.CollectionChanged += (s, e) => UpdateUnreadCount();
        }

        public void AddNotification(Notification notification)
        {
            // Ajouter au début de la liste (plus récent en premier)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _notifications.Insert(0, notification);
                UpdateUnreadCount();
            });
        }

        public void MarkAsRead(string notificationId)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null && !notification.IsRead)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    notification.IsRead = true;
                    UpdateUnreadCount();
                });
            }
        }

        public void MarkAllAsRead()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var notification in _notifications.Where(n => !n.IsRead))
                {
                    notification.IsRead = true;
                }
                UpdateUnreadCount();
            });
        }

        public void RemoveNotification(string notificationId)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _notifications.Remove(notification);
                    UpdateUnreadCount();
                });
            }
        }

        public void ClearAll()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _notifications.Clear();
                UpdateUnreadCount();
            });
        }

        private void UpdateUnreadCount()
        {
            UnreadCount = _notifications.Count(n => !n.IsRead);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
