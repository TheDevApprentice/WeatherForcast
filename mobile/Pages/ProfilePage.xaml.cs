using mobile.PageModels;

namespace mobile.Pages
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage(ProfilePageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            // Initialiser le switch selon le thème actuel
            ThemeSwitch.IsToggled = Application.Current?.UserAppTheme == AppTheme.Dark;
        }

        private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
            }
        }
    }
}
