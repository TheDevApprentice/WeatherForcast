using MediatR;

namespace domain.Events.Admin
{
    /// <summary>
    /// Événement déclenché quand les claims d'un utilisateur changent
    /// </summary>
    public class UserClaimChangedEvent : INotification
    {
        public string UserId { get; }
        public string Email { get; }
        public string ClaimType { get; }
        public string ClaimValue { get; }
        public bool IsAdded { get; } // true = ajouté, false = retiré
        public DateTime ChangedAt { get; }
        public string? ChangedBy { get; } // Admin qui a fait le changement

        public UserClaimChangedEvent(
            string userId,
            string email,
            string claimType,
            string claimValue,
            bool isAdded,
            string? changedBy = null)
        {
            UserId = userId;
            Email = email;
            ClaimType = claimType;
            ClaimValue = claimValue;
            IsAdded = isAdded;
            ChangedAt = DateTime.UtcNow;
            ChangedBy = changedBy;
        }
    }
}
