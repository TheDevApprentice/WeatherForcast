namespace domain.Events.Admin
{
    /// <summary>
    /// Événement déclenché quand une API Key est créée
    /// </summary>
    public class ApiKeyCreatedEvent : INotification
    {
        public int ApiKeyId { get; }
        public string UserId { get; }
        public string Email { get; }
        public string KeyName { get; }
        public DateTime CreatedAt { get; }
        public DateTime? ExpiresAt { get; }

        public ApiKeyCreatedEvent(
            int apiKeyId,
            string userId,
            string email,
            string keyName,
            DateTime? expiresAt = null)
        {
            ApiKeyId = apiKeyId;
            UserId = userId;
            Email = email;
            KeyName = keyName;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = expiresAt;
        }
    }
}
