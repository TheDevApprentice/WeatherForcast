using domain.Entities;

namespace domain.Interfaces.Services
{
    /// <summary>
    /// Service de génération de tokens JWT
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Générer un token JWT pour un utilisateur
        /// </summary>
        string GenerateToken(ApplicationUser user);
    }
}
