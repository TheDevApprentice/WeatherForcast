using MediatR;

namespace domain.Events.Admin
{
    /// <summary>
    /// Événement déclenché quand une nouvelle session est créée
    /// </summary>
    public class SessionCreatedEvent : INotification
    {
        public string SessionId { get; }
        public string UserId { get; }
        public string Email { get; }
        public DateTime CreatedAt { get; }
        public DateTime ExpiresAt { get; }
        public string? IpAddress { get; }
        public string? UserAgent { get; }

        public SessionCreatedEvent(
            string sessionId,
            string userId,
            string email,
            DateTime expiresAt,
            string? ipAddress = null,
            string? userAgent = null)
        {
            SessionId = sessionId;
            UserId = userId;
            Email = email;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = expiresAt;
            IpAddress = ipAddress;
            UserAgent = userAgent;
        }
    }
}
