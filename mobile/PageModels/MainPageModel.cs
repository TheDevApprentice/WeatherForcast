using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Services;

namespace mobile.PageModels
{
    public partial class MainPageModel : ObservableObject
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly IApiService _apiService;

        [ObservableProperty]
        private string welcomeMessage = "Bienvenue !";

        [ObservableProperty]
        private string userEmail = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        public MainPageModel(ISecureStorageService secureStorage, IApiService apiService)
        {
            _secureStorage = secureStorage;
            _apiService = apiService;
            LoadUserInfo();
        }

        private async void LoadUserInfo()
        {
            var userInfo = await _secureStorage.GetUserInfoAsync();
            UserEmail = userInfo.Email;
            WelcomeMessage = $"Bienvenue {userInfo.FirstName} {userInfo.LastName} !";
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            try
            {
                IsLoading = true;

                // Appeler l'API pour déconnecter (invalider la session côté serveur)
                await _apiService.LogoutAsync();

                // Supprimer toutes les données stockées localement
                await _secureStorage.ClearAllAsync();

                // Retourner à la page de connexion
                await Shell.Current.GoToAsync("///login");
            }
            catch (Exception ex)
            {
                // En cas d'erreur, supprimer quand même les données locales
                await _secureStorage.ClearAllAsync();
                await Shell.Current.GoToAsync("///login");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
