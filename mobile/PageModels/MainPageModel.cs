using CommunityToolkit.Mvvm.ComponentModel;
using mobile.Services;

namespace mobile.PageModels
{
    public partial class MainPageModel : ObservableObject
    {
        private readonly ISecureStorageService _secureStorage;

        [ObservableProperty]
        private string welcomeMessage = "Bienvenue !";

        public MainPageModel(ISecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
            LoadUserInfo();
        }

        private async void LoadUserInfo()
        {
            var userInfo = await _secureStorage.GetUserInfoAsync();
            WelcomeMessage = $"Bienvenue {userInfo.FirstName} {userInfo.LastName} !";
        }
    }
}
