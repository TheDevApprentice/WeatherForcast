namespace mobile.Services
{
    /// <summary>
    /// Interface pour le service de validation de session au démarrage
    /// </summary>
    public interface ISessionValidationService
    {
        Task<bool> ValidateSessionAsync();
        Task ClearSessionAsync();
    }
}
