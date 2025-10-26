namespace domain.Events.Admin
{
    /// <summary>
    /// Événement déclenché quand une API Key est révoquée
    /// </summary>
    public class ApiKeyRevokedEvent : INotification
    {
        public int ApiKeyId { get; }
        public string UserId { get; }
        public string Email { get; }
        public string KeyName { get; }
        public DateTime RevokedAt { get; }
        public string? RevokedBy { get; } // Admin qui a révoqué

        public ApiKeyRevokedEvent(
            int apiKeyId,
            string userId,
            string email,
            string keyName,
            string? revokedBy = null)
        {
            ApiKeyId = apiKeyId;
            UserId = userId;
            Email = email;
            KeyName = keyName;
            RevokedAt = DateTime.UtcNow;
            RevokedBy = revokedBy;
        }
    }
}
