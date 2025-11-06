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
            // Retour haptique
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            if (sender is Border border && border.BindingContext is Models.SavedUserProfile profile)
            {
                _viewModel.SelectProfileCommand.Execute(profile);
            }
        }

        private void OnDeleteProfileClicked(object sender, EventArgs e)
        {
            // Retour haptique
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            if (sender is ImageButton button && button.BindingContext is Models.SavedUserProfile profile)
            {
                _viewModel.RemoveProfileCommand.Execute(profile);
            }
        }

        private void OnUseAnotherAccountClicked(object sender, EventArgs e)
        {
            // Retour haptique pour le lien de navigation
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }

        private void OnBackToProfilesClicked(object sender, EventArgs e)
        {
            // Retour haptique pour le bouton retour
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }

        private void OnNavigateToRegisterClicked(object sender, EventArgs e)
        {
            // Retour haptique pour le lien de navigation
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
    }
}
