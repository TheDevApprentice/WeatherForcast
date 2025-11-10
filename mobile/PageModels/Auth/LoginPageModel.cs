using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using domain.DTOs.Auth;
using mobile.Services.Api.Interfaces;
using mobile.Services.Internal.Interfaces;
using System.Collections.ObjectModel;

namespace mobile.PageModels.Auth
{
    public partial class LoginPageModel : ObservableObject
    {
        private readonly IApiAuthService _apiAuthService;
        private readonly ISecureStorageService _secureStorage;
        private readonly IAuthenticationStateService _authState;
        private readonly ISavedProfilesService _savedProfiles;
        private readonly INetworkMonitorService _networkMonitor;

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

        [ObservableProperty]
        private bool isNetworkAvailable = true;

        public bool IsNotLoading => !IsLoading;
        public bool ShowClassicLogin => !ShowProfileSelection && SelectedProfile == null;
        public bool CanLogin => IsNetworkAvailable && !IsLoading;

        public LoginPageModel (
            IApiAuthService apiAuthService,
            ISecureStorageService secureStorage,
            IAuthenticationStateService authState,
            ISavedProfilesService savedProfilesService,
            INetworkMonitorService networkMonitor)
        {
            _apiAuthService = apiAuthService;
            _secureStorage = secureStorage;
            _authState = authState;
            _savedProfiles = savedProfilesService;
            _networkMonitor = networkMonitor;

            // S'abonner aux changements de connectivité
            _networkMonitor.ConnectivityChanged += OnConnectivityChanged;

            // Initialiser l'état réseau
            IsNetworkAvailable = _networkMonitor.IsNetworkAvailable;
        }

        private void OnConnectivityChanged (object? sender, NetworkAccess access)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsNetworkAvailable = access == NetworkAccess.Internet;
            });
        }

        [RelayCommand]
        private async Task LoginAsync ()
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

                var response = await _apiAuthService.LoginAsync(request);

                if (response != null)
                {
                    // Sauvegarder le token et les infos utilisateur
                    await _secureStorage.SaveTokenAsync(response.Token);
                    await _secureStorage.SaveUserInfoAsync(response.Email, response.FirstName, response.LastName);

                    // Extraire l'ID utilisateur du token JWT
                    var userInfo = await _secureStorage.GetUserInfoFromTokenAsync();

                    if (userInfo.HasValue)
                    {
                        // Sauvegarder l'état d'authentification centralisé
                        var authState = Models.AuthenticationState.Authenticated(
                            userInfo.Value.UserId,
                            userInfo.Value.Email,
                            userInfo.Value.FirstName,
                            userInfo.Value.LastName
                        );
                        await _authState.SetStateAsync(authState);

                        // Sauvegarder le profil pour la reconnexion rapide
                        try
                        {
                            var profile = new SavedUserProfile
                            {
                                Email = userInfo.Value.Email,
                                FirstName = userInfo.Value.FirstName,
                                LastName = userInfo.Value.LastName,
                                LastLoginDate = DateTime.Now
                            };
                            await _savedProfiles.SaveProfileAsync(profile);
                        }
                        catch (Exception profileEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Erreur sauvegarde profil: {profileEx.Message}");
#if DEBUG
                            await Shell.Current.DisplayAlert("Debug", $"Erreur sauvegarde profil login: {profileEx.Message}", "OK");
#endif
                            // Ne pas bloquer le login si la sauvegarde du profil échoue
                        }
                    }

                    // Mettre à jour l'UI du Shell
                    if (Shell.Current is AppShell appShell)
                    {
                        appShell.UpdateAuthenticationUI(true);
                    }

                    // Navigation vers l'application principale
#if ANDROID || IOS
                    // Sur mobile : réafficher le TabBar
                    Shell.SetTabBarIsVisible(Shell.Current, true);
#endif
                    // Navigation vers le Tab Dashboard (fonctionne sur toutes les plateformes)
                    await Shell.Current.GoToAsync("///main");
                }
                else
                {
                    ShowError("Email ou mot de passe incorrect");
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug", $"Erreur login: {ex.Message}\n{ex.StackTrace}", "OK");
#endif
                ShowError($"Erreur de connexion : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToRegisterAsync ()
        {
            // Navigation relative (fonctionne sur toutes les plateformes)
            await Shell.Current.GoToAsync("//register");
        }

        private void ShowError (string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        partial void OnIsLoadingChanged (bool value)
        {
            OnPropertyChanged(nameof(IsNotLoading));
            OnPropertyChanged(nameof(CanLogin));
        }

        partial void OnIsNetworkAvailableChanged (bool value)
        {
            OnPropertyChanged(nameof(CanLogin));
        }

        partial void OnShowProfileSelectionChanged (bool value)
        {
            OnPropertyChanged(nameof(ShowClassicLogin));
        }

        partial void OnSelectedProfileChanged (SavedUserProfile? value)
        {
            OnPropertyChanged(nameof(ShowClassicLogin));
        }

        /// <summary>
        /// Charge les profils sauvegardés au démarrage de la page
        /// </summary>
        public async Task LoadSavedProfilesAsync ()
        {
            try
            {
                var profiles = await _savedProfiles.GetSavedProfilesAsync();
                SavedProfiles = new ObservableCollection<SavedUserProfile>(profiles);
                HasSavedProfiles = profiles.Count > 0;
                ShowProfileSelection = HasSavedProfiles;

                System.Diagnostics.Debug.WriteLine($"🟢 Profils chargés : {profiles.Count}");
                System.Diagnostics.Debug.WriteLine($"🟢 ShowProfileSelection = {ShowProfileSelection}");
                System.Diagnostics.Debug.WriteLine($"🟢 HasSavedProfiles = {HasSavedProfiles}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur chargement profils: {ex.Message}");
                // Alerte active même en Release pour debug publish
                await Shell.Current.DisplayAlert("Debug Login", $"Erreur chargement profils: {ex.Message}\n{ex.GetType().Name}", "OK");
                SavedProfiles = new ObservableCollection<SavedUserProfile>();
                HasSavedProfiles = false;
                ShowProfileSelection = false;
            }
        }

        /// <summary>
        /// Sélectionne un profil pour se connecter
        /// </summary>
        [RelayCommand]
        private void SelectProfile (SavedUserProfile profile)
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
        private void BackToProfiles ()
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
        private void UseAnotherAccount ()
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
        private async Task RemoveProfile (SavedUserProfile profile)
        {
            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur suppression profil: {ex.Message}");
#if DEBUG
                await Shell.Current.DisplayAlert("Debug", $"Erreur suppression profil: {ex.Message}", "OK");
#endif
            }
        }
    }
}
