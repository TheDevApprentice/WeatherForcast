using mobile.PageModels.Auth;

namespace mobile.Pages.Auth
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginPageModel _viewModel;

        public LoginPage(LoginPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Charger les profils sauvegardés
            await _viewModel.LoadSavedProfilesAsync();
        }

        private void OnProfileCardTapped(object sender, TappedEventArgs e)
        {
            if (sender is Border border && border.BindingContext is Models.SavedUserProfile profile)
            {
                _viewModel.SelectProfileCommand.Execute(profile);
            }
        }

        private void OnDeleteProfileClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton button && button.BindingContext is Models.SavedUserProfile profile)
            {
                _viewModel.RemoveProfileCommand.Execute(profile);
            }
        }
    }
}
