using mobile.Models;

namespace mobile.Services
{
    /// <summary>
    /// Service de gestion des profils utilisateurs sauvegardés localement
    /// </summary>
    public interface ISavedProfilesService
    {
        /// <summary>
        /// Récupère la liste des profils sauvegardés, triés par date de dernière connexion (plus récent en premier)
        /// </summary>
        Task<List<SavedUserProfile>> GetSavedProfilesAsync();

        /// <summary>
        /// Sauvegarde ou met à jour un profil utilisateur
        /// Limite à 3 profils maximum, supprime le plus ancien si nécessaire
        /// </summary>
        Task SaveProfileAsync(SavedUserProfile profile);

        /// <summary>
        /// Supprime un profil spécifique par email
        /// </summary>
        Task RemoveProfileAsync(string email);

        /// <summary>
        /// Supprime tous les profils sauvegardés
        /// </summary>
        Task ClearAllProfilesAsync();

        /// <summary>
        /// Vérifie si des profils sont sauvegardés
        /// </summary>
        Task<bool> HasSavedProfilesAsync();

        /// <summary>
        /// Récupère un profil spécifique par email
        /// </summary>
        Task<SavedUserProfile?> GetProfileByEmailAsync(string email);
    }
}
