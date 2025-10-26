namespace domain.Events.Admin
{
    /// <summary>
    /// Événement déclenché quand une session est révoquée/déconnectée de force
    /// </summary>
    public class SessionRevokedEvent : INotification
    {
        public string SessionId { get; }
        public string UserId { get; }
        public string Email { get; }
        public DateTime RevokedAt { get; }
        public string? Reason { get; }
        public string? RevokedBy { get; }

        public SessionRevokedEvent(
            string sessionId,
            string userId,
            string email,
            string? reason = null,
            string? revokedBy = null)
        {
            SessionId = sessionId;
            UserId = userId;
            Email = email;
            RevokedAt = DateTime.UtcNow;
            Reason = reason;
            RevokedBy = revokedBy;
        }
    }
}
