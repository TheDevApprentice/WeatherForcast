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
            
            // S'abonner aux changements d'erreur pour déclencher l'animation
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.HasError) && _viewModel.HasError)
            {
                // Déclencher l'animation de shake sur erreur
                MainThread.BeginInvokeOnMainThread(async () => await ShakeErrorMessage());
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Charger les profils sauvegardés
            await _viewModel.LoadSavedProfilesAsync();
            
            // Animation d'entrée
            await AnimateViewEntry();
        }

        async Task AnimateViewEntry()
        {
            var profileView = this.FindByName<VerticalStackLayout>("ProfileSelectionView");
            var selectedView = this.FindByName<VerticalStackLayout>("SelectedProfileView");
            var classicView = this.FindByName<VerticalStackLayout>("ClassicLoginView");

            // Déterminer quelle vue est visible
            View? activeView = null;
            if (profileView?.IsVisible == true) activeView = profileView;
            else if (selectedView?.IsVisible == true) activeView = selectedView;
            else if (classicView?.IsVisible == true) activeView = classicView;

            if (activeView != null)
            {
                activeView.Opacity = 0;
                activeView.TranslationY = 20;
                await Task.WhenAll(
                    activeView.FadeTo(1, 400, Easing.CubicOut),
                    activeView.TranslateTo(0, 0, 400, Easing.CubicOut)
                );
            }
        }

        private async void OnProfileCardTapped(object sender, TappedEventArgs e)
        {
            // Retour haptique
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            if (sender is Border border && border.BindingContext is Models.SavedUserProfile profile)
            {
                // Animation de scale sur la carte
                await border.ScaleTo(0.95, 100, Easing.CubicOut);
                await border.ScaleTo(1.0, 100, Easing.CubicOut);
                
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

        // Animations de pressed/released pour les boutons
        async void OnButtonPressed(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                await button.ScaleTo(0.95, 100, Easing.CubicOut);
            }
        }

        async void OnButtonReleased(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                await button.ScaleTo(1.0, 100, Easing.CubicOut);
            }
        }

        // Animations pour les liens
        async void OnLinkPressed(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                await button.FadeTo(0.6, 100);
            }
        }

        async void OnLinkReleased(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                await button.FadeTo(1.0, 100);
            }
        }

        // Animation de shake sur erreur
        async Task ShakeErrorMessage()
        {
            var profileView = this.FindByName<VerticalStackLayout>("ProfileSelectionView");
            var selectedView = this.FindByName<VerticalStackLayout>("SelectedProfileView");
            var classicView = this.FindByName<VerticalStackLayout>("ClassicLoginView");

            // Déterminer quelle vue est visible
            View? activeView = null;
            if (profileView?.IsVisible == true) activeView = profileView;
            else if (selectedView?.IsVisible == true) activeView = selectedView;
            else if (classicView?.IsVisible == true) activeView = classicView;

            if (activeView != null)
            {
                // Animation de shake (gauche-droite)
                await activeView.TranslateTo(-15, 0, 50);
                await activeView.TranslateTo(15, 0, 50);
                await activeView.TranslateTo(-10, 0, 50);
                await activeView.TranslateTo(10, 0, 50);
                await activeView.TranslateTo(-5, 0, 50);
                await activeView.TranslateTo(0, 0, 50);
            }
        }

        void OnEmailCompleted(object sender, EventArgs e)
        {
            PasswordEntryClassic?.Focus();
        }
    }
}
