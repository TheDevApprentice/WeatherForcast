namespace mobile.Services
{
    /// <summary>
    /// Interface pour le stockage sécurisé (JWT Token)
    /// </summary>
    public interface ISecureStorageService
    {
        Task SaveTokenAsync(string token);
        Task<string?> GetTokenAsync();
        Task RemoveTokenAsync();
        Task<bool> IsAuthenticatedAsync();
        
        Task SaveUserInfoAsync(string email, string firstName, string lastName);
        Task<(string Email, string FirstName, string LastName)> GetUserInfoAsync();
        Task ClearAllAsync();
    }
}
