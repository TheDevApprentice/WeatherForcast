using mobile.Services.Internal.Interfaces;

namespace mobile.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly ISecureStorageService _secureStorage;

        public MainPage(MainPageModel viewModel, ISecureStorageService secureStorage)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _secureStorage = secureStorage;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Vérifier l'authentification à chaque fois que la page apparaît
            var isAuthenticated = await _secureStorage.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                // Rediriger vers la page de connexion si non authentifié
                await Shell.Current.GoToAsync("///login");
            }
        }
    }
}
