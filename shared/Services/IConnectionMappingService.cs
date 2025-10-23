namespace shared.Services
{
    /// <summary>
    /// Service pour mapper les utilisateurs à leurs ConnectionIds SignalR
    /// Utilisé pour exclure l'émetteur des notifications (Web et Mobile)
    /// </summary>
    public interface IConnectionMappingService
    {
        /// <summary>
        /// Ajouter un mapping userId → connectionId
        /// </summary>
        Task AddConnectionAsync(string userId, string connectionId);

        /// <summary>
        /// Retirer un mapping userId → connectionId
        /// </summary>
        Task RemoveConnectionAsync(string userId, string connectionId);

        /// <summary>
        /// Récupérer le ConnectionId d'un utilisateur
        /// </summary>
        Task<string?> GetConnectionIdAsync(string userId);
    }
}
