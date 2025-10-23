using MediatR;

namespace domain.Events.Admin
{
    /// <summary>
    /// Événement déclenché quand les rôles d'un utilisateur changent
    /// </summary>
    public class UserRoleChangedEvent : INotification
    {
        public string UserId { get; }
        public string Email { get; }
        public string RoleName { get; }
        public bool IsAdded { get; } // true = ajouté, false = retiré
        public DateTime ChangedAt { get; }
        public string? ChangedBy { get; } // Admin qui a fait le changement

        public UserRoleChangedEvent(
            string userId,
            string email,
            string roleName,
            bool isAdded,
            string? changedBy = null)
        {
            UserId = userId;
            Email = email;
            RoleName = roleName;
            IsAdded = isAdded;
            ChangedAt = DateTime.UtcNow;
            ChangedBy = changedBy;
        }
    }
}
