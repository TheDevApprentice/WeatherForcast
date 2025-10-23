using MediatR;

namespace domain.Events.Admin
{
    /// <summary>
    /// Événement déclenché quand un utilisateur se connecte
    /// </summary>
    public class UserLoggedInEvent : INotification
    {
        public string UserId { get; }
        public string Email { get; }
        public string? UserName { get; }
        public DateTime LoggedInAt { get; }
        public string? IpAddress { get; }
        public string? UserAgent { get; }

        public UserLoggedInEvent(
            string userId,
            string email,
            string? userName = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            UserId = userId;
            Email = email;
            UserName = userName;
            LoggedInAt = DateTime.UtcNow;
            IpAddress = ipAddress;
            UserAgent = userAgent;
        }
    }
}
