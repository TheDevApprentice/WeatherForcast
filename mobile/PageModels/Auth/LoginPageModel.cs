using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Models;
using mobile.Models.DTOs;
using mobile.Services;
using System.Collections.ObjectModel;

namespace mobile.PageModels.Auth
{
    public partial class LoginPageModel : ObservableObject
    {
        private readonly IApiService _apiService;
        private readonly ISecureStorageService _secureStorage;
        private readonly IAuthenticationStateService _authState;
        private readonly ISavedProfilesService _savedProfiles;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasError;

        [ObservableProperty]
        private ObservableCollection<SavedUserProfile> savedProfiles = new();

        [ObservableProperty]
        private bool hasSavedProfiles;

        [ObservableProperty]
        private SavedUserProfile? selectedProfile;

        [ObservableProperty]
        private bool showProfileSelection = true;

        public bool IsNotLoading => !IsLoading;
        public bool ShowClassicLogin => !ShowProfileSelection && SelectedProfile == null;

        public LoginPageModel(
            IApiService apiService, 
            ISecureStorageService secureStorage,
            IAuthenticationStateService authState,
            ISavedProfilesService savedProfilesService)
        {
            _apiService = apiService;
            _secureStorage = secureStorage;
            _authState = authState;
            _savedProfiles = savedProfilesService;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            // Réinitialiser l'erreur
            HasError = false;
            ErrorMessage = string.Empty;

            // Validation
            if (string.IsNullOrWhiteSpace(Email))
            {
                ShowError("Veuillez saisir votre email");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Veuillez saisir votre mot de passe");
                return;
            }

            try
            {
                IsLoading = true;

                var request = new LoginRequest
                {
                    Email = Email.Trim(),
                    Password = Password
                };

                var response = await _apiService.LoginAsync(request);

                if (response != null)
                {
                    // Sauvegarder le token et les infos utilisateur
                    await _secureStorage.SaveTokenAsync(response.Token);
                    await _secureStorage.SaveUserInfoAsync(response.Email, response.FirstName, response.LastName);

                    // Récupérer l'ID utilisateur depuis l'API
                    var currentUser = await _apiService.GetCurrentUserAsync();
                    
                    if (currentUser != null)
                    {
                        // Sauvegarder l'état d'authentification centralisé
                        var authState = Models.AuthenticationState.Authenticated(
                            currentUser.Id,
                            currentUser.Email,
                            currentUser.FirstName,
                            currentUser.LastName
                        );
                        await _authState.SetStateAsync(authState);

                        // Sauvegarder le profil pour la reconnexion rapide
                        var profile = new SavedUserProfile
                        {
                            Email = currentUser.Email,
                            FirstName = currentUser.FirstName,
                            LastName = currentUser.LastName,
                            LastLoginDate = DateTime.Now
                        };
                        await _savedProfiles.SaveProfileAsync(profile);
                    }

                    // Mettre à jour l'UI du Shell
                    if (Shell.Current is AppShell appShell)
                    {
                        appShell.UpdateAuthenticationUI(true);
                    }

                    // Navigation vers l'application principale
                    await Shell.Current.GoToAsync("///main");
                }
                else
                {
                    ShowError("Email ou mot de passe incorrect");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Erreur de connexion : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToRegisterAsync()
        {
            await Shell.Current.GoToAsync("register");
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        partial void OnIsLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotLoading));
        }

        partial void OnShowProfileSelectionChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowClassicLogin));
        }

        partial void OnSelectedProfileChanged(SavedUserProfile? value)
        {
            OnPropertyChanged(nameof(ShowClassicLogin));
        }

        /// <summary>
        /// Charge les profils sauvegardés au démarrage de la page
        /// </summary>
        public async Task LoadSavedProfilesAsync()
        {
            var profiles = await _savedProfiles.GetSavedProfilesAsync();
            SavedProfiles = new ObservableCollection<SavedUserProfile>(profiles);
            HasSavedProfiles = profiles.Count > 0;
            ShowProfileSelection = HasSavedProfiles;
            
            System.Diagnostics.Debug.WriteLine($"🟢 Profils chargés : {profiles.Count}");
            System.Diagnostics.Debug.WriteLine($"🟢 ShowProfileSelection = {ShowProfileSelection}");
            System.Diagnostics.Debug.WriteLine($"🟢 HasSavedProfiles = {HasSavedProfiles}");
        }

        /// <summary>
        /// Sélectionne un profil pour se connecter
        /// </summary>
        [RelayCommand]
        private void SelectProfile(SavedUserProfile profile)
        {
            System.Diagnostics.Debug.WriteLine($"🔵 SelectProfile appelé pour : {profile.Email}");
            SelectedProfile = profile;
            Email = profile.Email;
            Password = string.Empty;
            ShowProfileSelection = false;
            HasError = false;
            ErrorMessage = string.Empty;
            System.Diagnostics.Debug.WriteLine($"🔵 ShowProfileSelection = {ShowProfileSelection}, SelectedProfile = {SelectedProfile?.Email}");
        }

        /// <summary>
        /// Retour à la sélection des profils
        /// </summary>
        [RelayCommand]
        private void BackToProfiles()
        {
            SelectedProfile = null;
            Email = string.Empty;
            Password = string.Empty;
            ShowProfileSelection = true;
            HasError = false;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Utiliser un autre compte (affiche le formulaire classique)
        /// </summary>
        [RelayCommand]
        private void UseAnotherAccount()
        {
            System.Diagnostics.Debug.WriteLine($"🟡 UseAnotherAccount appelé");
            SelectedProfile = null;
            Email = string.Empty;
            Password = string.Empty;
            ShowProfileSelection = false;
            HasError = false;
            ErrorMessage = string.Empty;
            System.Diagnostics.Debug.WriteLine($"🟡 ShowProfileSelection = {ShowProfileSelection}");
        }

        /// <summary>
        /// Supprime un profil sauvegardé
        /// </summary>
        [RelayCommand]
        private async Task RemoveProfile(SavedUserProfile profile)
        {
            System.Diagnostics.Debug.WriteLine($"🔴 RemoveProfile appelé pour : {profile.Email}");
            await _savedProfiles.RemoveProfileAsync(profile.Email);
            SavedProfiles.Remove(profile);
            HasSavedProfiles = SavedProfiles.Count > 0;
            
            if (!HasSavedProfiles)
            {
                ShowProfileSelection = false;
            }
            System.Diagnostics.Debug.WriteLine($"🔴 Profil supprimé. Reste : {SavedProfiles.Count}");
        }
    }
}
