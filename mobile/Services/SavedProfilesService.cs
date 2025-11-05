using mobile.Models;
using System.Text.Json;

namespace mobile.Services
{
    /// <summary>
    /// Implémentation du service de gestion des profils sauvegardés
    /// Stocke les profils dans SecureStorage sous forme de JSON
    /// </summary>
    public class SavedProfilesService : ISavedProfilesService
    {
        private const string PROFILES_KEY = "saved_user_profiles";
        private const int MAX_PROFILES = 3;

        public SavedProfilesService()
        {
        }

        public async Task<List<SavedUserProfile>> GetSavedProfilesAsync()
        {
            try
            {
                var json = await SecureStorage.GetAsync(PROFILES_KEY);
                
                if (string.IsNullOrEmpty(json))
                {
                    return new List<SavedUserProfile>();
                }

                var profiles = JsonSerializer.Deserialize<List<SavedUserProfile>>(json);
                
                // Trier par date de dernière connexion (plus récent en premier)
                return profiles?
                    .OrderByDescending(p => p.LastLoginDate)
                    .ToList() ?? new List<SavedUserProfile>();
            }
            catch (Exception ex)
            {
                // En cas d'erreur de désérialisation, retourner une liste vide
                System.Diagnostics.Debug.WriteLine($"❌ Erreur GetSavedProfilesAsync: {ex.Message}");
                // Alerte active même en Release pour debug publish
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("Debug Profils", $"Erreur lecture: {ex.Message}\n{ex.GetType().Name}", "OK");
                });
                return new List<SavedUserProfile>();
            }
        }

        public async Task SaveProfileAsync(SavedUserProfile profile)
        {
            try
            {
                var profiles = await GetSavedProfilesAsync();

                // Vérifier si le profil existe déjà (par email)
                var existingProfile = profiles.FirstOrDefault(p => 
                    p.Email.Equals(profile.Email, StringComparison.OrdinalIgnoreCase));

                if (existingProfile != null)
                {
                    // Mettre à jour le profil existant
                    existingProfile.FirstName = profile.FirstName;
                    existingProfile.LastName = profile.LastName;
                    existingProfile.LastLoginDate = profile.LastLoginDate;
                }
                else
                {
                    // Ajouter le nouveau profil
                    profiles.Add(profile);

                    // Si on dépasse la limite, supprimer le plus ancien
                    if (profiles.Count > MAX_PROFILES)
                    {
                        var oldestProfile = profiles.OrderBy(p => p.LastLoginDate).First();
                        profiles.Remove(oldestProfile);
                    }
                }

                // Sauvegarder dans SecureStorage
                var json = JsonSerializer.Serialize(profiles);
                await SecureStorage.SetAsync(PROFILES_KEY, json);
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas crasher l'application
                System.Diagnostics.Debug.WriteLine($"Error saving profile: {ex.Message}");
                // Alerte active même en Release pour debug publish
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("Debug Profils", $"Erreur sauvegarde: {ex.Message}\n{ex.GetType().Name}", "OK");
                });
            }
        }

        public async Task RemoveProfileAsync(string email)
        {
            try
            {
                var profiles = await GetSavedProfilesAsync();
                var profileToRemove = profiles.FirstOrDefault(p => 
                    p.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (profileToRemove != null)
                {
                    profiles.Remove(profileToRemove);
                    
                    if (profiles.Count > 0)
                    {
                        var json = JsonSerializer.Serialize(profiles);
                        await SecureStorage.SetAsync(PROFILES_KEY, json);
                    }
                    else
                    {
                        // Si plus de profils, supprimer la clé
                        SecureStorage.Remove(PROFILES_KEY);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas crasher l'application
                System.Diagnostics.Debug.WriteLine($"Error removing profile: {ex.Message}");
#if DEBUG
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("Debug", $"Erreur suppression profil: {ex.Message}", "OK");
                });
#endif
            }
        }

        public Task ClearAllProfilesAsync()
        {
            try
            {
                SecureStorage.Remove(PROFILES_KEY);
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas crasher l'application
                System.Diagnostics.Debug.WriteLine($"Error clearing profiles: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public async Task<bool> HasSavedProfilesAsync()
        {
            var profiles = await GetSavedProfilesAsync();
            return profiles.Count > 0;
        }

        public async Task<SavedUserProfile?> GetProfileByEmailAsync(string email)
        {
            var profiles = await GetSavedProfilesAsync();
            return profiles.FirstOrDefault(p => 
                p.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }
    }
}
