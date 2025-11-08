using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace mobile.Services
{
    /// <summary>
    /// Store centralisé pour gérer toutes les messages de l'application
    /// </summary>
    public interface IMessageStore : INotifyPropertyChanged
    {
        /// <summary>
        /// Collection observable de toutes les messages
        /// </summary>
        ObservableCollection<Message> Messages { get; }

        /// <summary>
        /// Nombre de messages non lues
        /// </summary>
        int UnreadCount { get; }

        /// <summary>
        /// Ajoute une nouvelle message au store
        /// </summary>
        void AddMessage (Message message);

        /// <summary>
        /// Marque une message comme lue
        /// </summary>
        void MarkAsRead (string messageId);

        /// <summary>
        /// Marque toutes les message comme lues
        /// </summary>
        void MarkAllAsRead ();

        /// <summary>
        /// Supprime une message
        /// </summary>
        void RemoveMessage (string messageId);

        /// <summary>
        /// Supprime toutes les message
        /// </summary>
        void ClearAll ();
    }

    /// <summary>
    /// Implémentation du store de notifications
    /// </summary>
    public class MessageStore : IMessageStore
    {
        private readonly ObservableCollection<Message> _messages = new();
        private int _unreadCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Message> Messages => _messages;

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

        public MessageStore ()
        {
            // S'abonner aux changements de la collection pour mettre à jour le compteur
            _messages.CollectionChanged += (s, e) => UpdateUnreadCount();
        }

        public void AddMessage (Message message)
        {
            // Ajouter au début de la liste (plus récent en premier)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _messages.Insert(0, message);
                UpdateUnreadCount();
            });
        }

        public void MarkAsRead (string messageId)
        {
            var message = _messages.FirstOrDefault(n => n.Id == messageId);
            if (message != null && !message.IsRead)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    message.IsRead = true;
                    UpdateUnreadCount();
                });
            }
        }

        public void MarkAllAsRead ()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var message in _messages.Where(n => !n.IsRead))
                {
                    message.IsRead = true;
                }
                UpdateUnreadCount();
            });
        }

        public void RemoveMessage (string messageId)
        {
            var message = _messages.FirstOrDefault(n => n.Id == messageId);
            if (message != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _messages.Remove(message);
                    UpdateUnreadCount();
                });
            }
        }

        public void ClearAll ()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _messages.Clear();
                UpdateUnreadCount();
            });
        }

        private void UpdateUnreadCount ()
        {
            UnreadCount = _messages.Count(n => !n.IsRead);
        }

        protected virtual void OnPropertyChanged ([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
