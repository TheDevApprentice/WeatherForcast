using System.ComponentModel;

namespace mobile.Models
{
    /// <summary>
    /// Modèle représentant une conversation entre utilisateurs
    /// </summary>
    public class Conversation : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>
        /// Identifiant unique de la conversation
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Titre de la conversation (optionnel, pour les groupes)
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Liste des membres de la conversation
        /// </summary>
        public List<ConversationMember> Members { get; set; } = new();

        /// <summary>
        /// Liste des messages de la conversation
        /// </summary>
        public List<Message> Messages { get; set; } = new();

        /// <summary>
        /// Dernier message de la conversation
        /// </summary>
        public Message? LastMessage => Messages.OrderByDescending(m => m.Timestamp).FirstOrDefault();

        /// <summary>
        /// Date et heure de la dernière activité
        /// </summary>
        public DateTime LastActivity => LastMessage?.Timestamp ?? CreatedAt;

        /// <summary>
        /// Date de création de la conversation
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Indique si la conversation est épinglée
        /// </summary>
        public bool IsPinned { get; set; } = false;

        /// <summary>
        /// Type de conversation (Support, Direct, Group)
        /// </summary>
        public ConversationType Type { get; set; } = ConversationType.Direct;

        /// <summary>
        /// Nombre de messages non lus dans cette conversation
        /// </summary>
        public int UnreadCount => Messages.Count(m => !m.IsRead);

        /// <summary>
        /// Indique si la conversation contient des messages non lus
        /// </summary>
        public bool HasUnreadMessages => UnreadCount > 0;

        /// <summary>
        /// Obtient le nom d'affichage de la conversation
        /// Pour une conversation directe, retourne le nom de l'autre membre
        /// Pour un groupe ou support, retourne le titre
        /// </summary>
        public string GetDisplayName(string currentUserId)
        {
            if (!string.IsNullOrEmpty(Title))
                return Title;

            if (Type == ConversationType.Direct)
            {
                var otherMember = Members.FirstOrDefault(m => m.UserId != currentUserId);
                return otherMember?.DisplayName ?? "Inconnu";
            }

            return "Conversation";
        }

        /// <summary>
        /// Obtient les initiales pour l'avatar de la conversation
        /// </summary>
        public string GetInitials(string currentUserId)
        {
            if (Type == ConversationType.Support)
                return "SP";

            if (Type == ConversationType.Direct)
            {
                var otherMember = Members.FirstOrDefault(m => m.UserId != currentUserId);
                return otherMember?.GetInitials() ?? "?";
            }

            // Pour les groupes, prendre les initiales du titre ou "GR"
            if (!string.IsNullOrEmpty(Title))
            {
                var words = Title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2)
                    return $"{words[0][0]}{words[1][0]}".ToUpper();
                if (words.Length == 1 && words[0].Length >= 2)
                    return words[0].Substring(0, 2).ToUpper();
            }

            return "GR";
        }

        /// <summary>
        /// Notifie les changements de propriété pour le binding
        /// </summary>
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // Si les messages changent, notifier aussi les propriétés calculées
            if (propertyName == nameof(Messages))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnreadCount)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUnreadMessages)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessage)));
            }
        }
    }

    /// <summary>
    /// Type de conversation
    /// </summary>
    public enum ConversationType
    {
        /// <summary>
        /// Conversation directe entre deux utilisateurs
        /// </summary>
        Direct,

        /// <summary>
        /// Conversation de groupe
        /// </summary>
        Group,

        /// <summary>
        /// Conversation avec le support
        /// </summary>
        Support
    }
}
