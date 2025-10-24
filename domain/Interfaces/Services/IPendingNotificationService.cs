namespace domain.Interfaces.Services
{
    public interface IPendingNotificationService
    {
        Task AddAsync(string channel, string key, string type, string payloadJson, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<(string Type, string PayloadJson)>> FetchPendingAsync(string channel, string key, CancellationToken cancellationToken = default);
    }
}
