using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using domain.DTOs.Auth;
using mobile.Services.Api.Interfaces;
using mobile.Services.Internal.Interfaces;
using mobile.Services.Notifications.Interfaces;
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
        private readonly INotificationService _notificationService;

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
            INetworkMonitorService networkMonitor,
            INotificationService notificationService)
        {
            _apiAuthService = apiAuthService;
            _secureStorage = secureStorage;
            _authState = authState;
            _savedProfiles = savedProfilesService;
            _networkMonitor = networkMonitor;
            _notificationService = notificationService;

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
            ResetError();

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
                    if(response.Token != null)
                    {
                        await _secureStorage.SaveTokenAsync(response.Token);
                    }

                    await _secureStorage.SaveUserInfoAsync(response.Email, response.FirstName, response.LastName);

                    // Extraire l'ID utilisateur du token JWT
                    var userId = await _secureStorage.GetUserIdFromTokenAsync();
                    var userInfo = await _secureStorage.GetUserInfoAsync();

                    if (!string.IsNullOrEmpty(userId))
                    {
                        // Sauvegarder l'état d'authentification centralisé
                        var authState = AuthenticationState.Authenticated(
                            userId,
                            userInfo.Email,
                            userInfo.FirstName,
                            userInfo.LastName
                        );
                        await _authState.SetStateAsync(authState);

                        // Sauvegarder le profil pour la reconnexion rapide
                        try
                        {
                            var profile = new SavedUserProfile
                            {
                                Email = userInfo.Email,
                                FirstName = userInfo.FirstName,
                                LastName = userInfo.LastName,
                                LastLoginDate = DateTime.Now
                            };
                            await _savedProfiles.SaveProfileAsync(profile);
                        }
                        catch (Exception profileEx)
                        {
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
            _notificationService.ShowErrorAsync(ErrorMessage);
        }
        private void ResetError ()
        {
            ErrorMessage = string.Empty;
            HasError = false;
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

                // Profils chargés avec succès
            }
            catch (Exception ex)
            {
                // Alerte active même en Release pour debug publish
#if DEBUG
                await Shell.Current.DisplayAlert("Debug Login", $"Erreur chargement profils: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
            SelectedProfile = profile;
            Email = profile.Email;
            Password = string.Empty;
            ShowProfileSelection = false;
            // Réinitialiser l'erreur
            ResetError();
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
            // Réinitialiser l'erreur
            ResetError();
        }

        /// <summary>
        /// Utiliser un autre compte (affiche le formulaire classique)
        /// </summary>
        [RelayCommand]
        private void UseAnotherAccount ()
        {
            SelectedProfile = null;
            Email = string.Empty;
            Password = string.Empty;
            ShowProfileSelection = false;
            // Réinitialiser l'erreur
            ResetError();
        }

        /// <summary>
        /// Supprime un profil sauvegardé
        /// </summary>
        [RelayCommand]
        private async Task RemoveProfile (SavedUserProfile profile)
        {
            try
            {
                await _savedProfiles.RemoveProfileAsync(profile.Email);
                SavedProfiles.Remove(profile);
                await _notificationService.ShowSuccessAsync("Le profil à bien été supprimé");
                HasSavedProfiles = SavedProfiles.Count > 0;

                if (!HasSavedProfiles)
                {
                    ShowProfileSelection = false;
                }
                // Profil supprimé
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug", $"Erreur suppression profil: {ex.Message}", "OK");
#endif
            }
        }
    }
}
