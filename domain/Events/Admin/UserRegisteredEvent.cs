using MediatR;

namespace domain.Events.Admin
{
    /// <summary>
    /// Événement déclenché quand un nouvel utilisateur s'enregistre
    /// </summary>
    public class UserRegisteredEvent : INotification
    {
        public string UserId { get; }
        public string Email { get; }
        public string? UserName { get; }
        public DateTime RegisteredAt { get; }
        public string? IpAddress { get; }

        public UserRegisteredEvent(
            string userId, 
            string email, 
            string? userName = null,
            string? ipAddress = null)
        {
            UserId = userId;
            Email = email;
            UserName = userName;
            RegisteredAt = DateTime.UtcNow;
            IpAddress = ipAddress;
        }
    }
}
