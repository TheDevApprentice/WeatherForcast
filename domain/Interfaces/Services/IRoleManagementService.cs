using domain.Entities;
using System.Security.Claims;

namespace domain.Interfaces.Services
{
    /// <summary>
    /// Service de gestion des rôles et claims
    /// </summary>
    public interface IRoleManagementService
    {
        /// <summary>
        /// Assigner un rôle à un utilisateur
        /// </summary>
        Task<bool> AssignRoleAsync(string userId, string roleName);
        
        /// <summary>
        /// Retirer un rôle d'un utilisateur
        /// </summary>
        Task<bool> RemoveRoleAsync(string userId, string roleName);
        
        /// <summary>
        /// Obtenir les rôles d'un utilisateur
        /// </summary>
        Task<IList<string>> GetUserRolesAsync(string userId);
        
        /// <summary>
        /// Vérifier si un utilisateur a un rôle
        /// </summary>
        Task<bool> IsInRoleAsync(string userId, string roleName);
        
        /// <summary>
        /// Ajouter un claim à un utilisateur
        /// </summary>
        Task<bool> AddClaimAsync(string userId, string claimType, string claimValue);
        
        /// <summary>
        /// Retirer un claim d'un utilisateur
        /// </summary>
        Task<bool> RemoveClaimAsync(string userId, string claimType, string claimValue);
        
        /// <summary>
        /// Obtenir les claims d'un utilisateur
        /// </summary>
        Task<IList<Claim>> GetUserClaimsAsync(string userId);
        
        /// <summary>
        /// Vérifier si un utilisateur a une permission
        /// </summary>
        Task<bool> HasPermissionAsync(string userId, string permission);
        
        /// <summary>
        /// Obtenir tous les rôles disponibles
        /// </summary>
        Task<IList<string>> GetAllRolesAsync();
    }
}
