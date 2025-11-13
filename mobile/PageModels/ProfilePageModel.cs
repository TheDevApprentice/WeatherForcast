using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Services.Api.Interfaces;
using mobile.Services.Internal.Interfaces;
using mobile.Services.Notifications.Interfaces;

namespace mobile.PageModels
{
    public partial class ProfilePageModel : ObservableObject
    {
        private readonly IAuthenticationStateService _authStateService;
        private readonly INetworkMonitorService _networkMonitor;
        INotificationService _notificationService;

        [ObservableProperty]
        private string userName = "Utilisateur";

        [ObservableProperty]
        private string userEmail = "user@example.com";

        [ObservableProperty]
        private string initials = "U";

        [ObservableProperty]
        private bool isNetworkAvailable = true;

        public bool CanLogout => IsNetworkAvailable;

        // Sur desktop, le bouton de déconnexion est dans le Flyout
        public bool ShowLogoutButton
        {
            get
            {
#if ANDROID || IOS
                return true;
#else
                return false;
#endif
            }
        }

        // Sur desktop, le toggle de thème est dans le Flyout
        public bool ShowThemeToggle
        {
            get
            {
#if ANDROID || IOS
                return true;
#else
                return false;
#endif
            }
        }

        // Récupérer les services
        private readonly ISecureStorageService _secureStorage;
        private readonly IApiAuthService _apiAuthService;

        public ProfilePageModel (
            ISecureStorageService secureStorage,
            IApiAuthService apiAuthService,
            IAuthenticationStateService authStateService,
            INetworkMonitorService networkMonitor,
            INotificationService notificationService)
        {
            _authStateService = authStateService;
            _networkMonitor = networkMonitor;
            _notificationService = notificationService;
            _secureStorage = secureStorage;
            _apiAuthService = apiAuthService;

            // S'abonner aux changements de connectivité
            _networkMonitor.ConnectivityChanged += OnConnectivityChanged;

            // Initialiser l'état réseau
            IsNetworkAvailable = _networkMonitor.IsNetworkAvailable;

            LoadUserInfo();
        }

        private void OnConnectivityChanged (object? sender, NetworkAccess access)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsNetworkAvailable = access == NetworkAccess.Internet;
            });
        }

        partial void OnIsNetworkAvailableChanged (bool value)
        {
            OnPropertyChanged(nameof(CanLogout));

            // Notifier la commande pour qu'elle se réévalue
            LogoutCommand.NotifyCanExecuteChanged();
        }

        private async void LoadUserInfo ()
        {
            try
            {
                var authState = await _authStateService.GetStateAsync();
                if (authState.IsAuthenticated && authState.Email != null)
                {
                    UserName = authState.FirstName ?? "Utilisateur";
                    UserEmail = authState.Email ?? "user@example.com";
                    UpdateInitials();
                }
                else
                {
                    UpdateInitials();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ProfilePageModel", $"❌ Erreur lors du chargement des informations utilisateur: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                await _notificationService.ShowErrorAsync("Erreur lors du chargement des informations utilisateur");
            }
        }

        private void UpdateInitials ()
        {
            try
            {
                string source = !string.IsNullOrWhiteSpace(UserName) ? UserName : UserEmail;
                if (string.IsNullOrWhiteSpace(source))
                {
                    Initials = "U";
                    return;
                }

                // Si email, prendre la partie avant @
                var baseText = source.Contains('@') ? source.Split('@')[0] : source;
                var parts = baseText
                    .Replace("_", " ")
                    .Replace("-", " ")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0)
                {
                    Initials = char.ToUpper(baseText[0]).ToString();
                }
                else if (parts.Length == 1)
                {
                    var p = parts[0];
                    Initials = p.Length >= 2 ? ($"{char.ToUpper(p[0])}{char.ToUpper(p[1])}") : char.ToUpper(p[0]).ToString();
                }
                else
                {
                    Initials = $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}";
                }
            }
            catch
            {
                Initials = "U";
            }
        }

        [RelayCommand]
        private async Task OpenSettings ()
        {
            try
            {
                // Vérifier si un modal ParameterCenterPage est déjà ouvert
                var modalStack = Shell.Current.Navigation.ModalStack;
                var existingParameterPage = modalStack.FirstOrDefault(p => p is ParameterCenterPage);

                if (existingParameterPage != null)
                {
                    // Le modal est déjà ouvert, ne rien faire
                    return;
                }

                // Ouvrir le modal
                var parameterCenterPage = new ParameterCenterPage();
                await Shell.Current.Navigation.PushModalAsync(parameterCenterPage, animated: true);
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"Impossible d'ouvrir les paramètres: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanLogout))]
        private async Task Logout ()
        {
            try
            {
                var confirm = await Shell.Current.DisplayAlert(
                    "Déconnexion",
                    "Êtes-vous sûr de vouloir vous déconnecter ?",
                    "Oui",
                    "Non");

                if (confirm)
                {
                    if (_secureStorage != null && _authStateService != null && _apiAuthService != null)
                    {
                        // Appeler l'API pour déconnecter
                        await _apiAuthService.LogoutAsync();

                        // Supprimer les données locales
                        await _secureStorage.ClearAllAsync();

                        // Effacer l'état d'authentification centralisé
                        await _authStateService.ClearStateAsync();

                        // Rediriger vers la page de connexion
                        await Shell.Current.GoToAsync("///login");

                    }
                }
            }
            catch (Exception profileEx)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ProfilePageModel", $"❌ Erreur lors de la sauvegarde du profil avant logout: {profileEx.Message}\n{profileEx.GetType().Name}", "OK");
#endif
                await _notificationService.ShowErrorAsync("⚠️ Erreur lors de la sauvegarde du profil avant logout");
            }

            // Déconnexion réussie
        }
    }
}
