using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace mobile.PageModels
{
    public partial class ProfilePageModel : ObservableObject
    {
        private readonly IAuthenticationStateService _authStateService;
        private readonly ILogger<ProfilePageModel> _logger;

        [ObservableProperty]
        private string userName = "Utilisateur";

        [ObservableProperty]
        private string userEmail = "user@example.com";

        public ProfilePageModel(
            IAuthenticationStateService authStateService,
            ILogger<ProfilePageModel> logger)
        {
            _authStateService = authStateService;
            _logger = logger;

            LoadUserInfo();
        }

        private async void LoadUserInfo()
        {
            try
            {
                var authState = await _authStateService.GetStateAsync();
                if (authState.IsAuthenticated && authState.Email != null)
                {
                    UserName = authState.FirstName ?? "Utilisateur";
                    UserEmail = authState.Email ?? "user@example.com";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des informations utilisateur");
            }
        }

        [RelayCommand]
        private async Task OpenSettings()
        {
            // TODO: Implémenter la page de paramètres
            await Shell.Current.DisplayAlert("Paramètres", "Page de paramètres à venir", "OK");
        }

        [RelayCommand]
        private async Task Logout()
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
                    // Récupérer les services nécessaires
                    var secureStorage = Shell.Current.Handler?.MauiContext?.Services.GetService<ISecureStorageService>();
                    var apiService = Shell.Current.Handler?.MauiContext?.Services.GetService<IApiService>();

                    if (secureStorage != null && apiService != null)
                    {
                        // Appeler l'API pour déconnecter
                        await apiService.LogoutAsync();

                        // Supprimer les données locales
                        await secureStorage.ClearAllAsync();

                        // Effacer l'état d'authentification centralisé
                        await _authStateService.ClearStateAsync();

#if ANDROID || IOS
                        // Sur mobile : masquer le TabBar et afficher le login en modal
                        Shell.SetTabBarIsVisible(Shell.Current, false);
                        var loginPage = Shell.Current.Handler?.MauiContext?.Services.GetService<Pages.Auth.LoginPage>();
                        if (loginPage != null)
                        {
                            await Shell.Current.Navigation.PushModalAsync(loginPage, true);
                        }
#else
                        // Sur desktop : mettre à jour l'UI et naviguer vers login
                        if (Shell.Current is AppShell appShell)
                        {
                            appShell.UpdateAuthenticationUI(false);
                        }
                        await Shell.Current.GoToAsync("///login");
#endif

                        _logger.LogInformation("✅ Déconnexion réussie");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion");
                await Shell.Current.DisplayAlert("Erreur", "Une erreur est survenue lors de la déconnexion", "OK");
            }
        }
    }
}
