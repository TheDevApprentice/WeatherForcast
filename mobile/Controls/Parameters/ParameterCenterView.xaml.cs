using mobile.Services.Theme;

namespace mobile.Controls
{
    /// <summary>
    /// Centre de paramètres affichant les options configurables de l'application
    /// </summary>
    public partial class ParameterCenterView : ContentView
    {
        private readonly IThemeService? _themeService;

        public ParameterCenterView ()
        {
            InitializeComponent();

            // Récupérer le service de thème
            _themeService = Application.Current?.Handler?.MauiContext?.Services.GetService<IThemeService>();

            // Initialiser l'état du switch selon le thème actuel
            if (_themeService != null)
            {
                ThemeSwitch.IsToggled = _themeService.CurrentTheme == AppTheme.Dark;
            }
        }

        private async void OnThemeSwitchToggled (object sender, ToggledEventArgs e)
        {
            if (_themeService != null)
            {
                var newTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
                await _themeService.SetThemeAsync(newTheme, animated: true);
            }
        }

        private void OnThemeCardTapped (object sender, EventArgs e)
        {
            // Toggle le switch quand on clique sur la carte
            ThemeSwitch.IsToggled = !ThemeSwitch.IsToggled;
        }

        private async void OnCloseClicked (object sender, EventArgs e)
        {
            // Fermer la page modale parente
            var parentPage = GetParentPage();
            if (parentPage?.Navigation != null)
            {
                await parentPage.Navigation.PopModalAsync();
            }
        }

        private Page? GetParentPage ()
        {
            Element? parent = this.Parent;
            while (parent != null)
            {
                if (parent is Page page)
                    return page;
                parent = parent.Parent;
            }
            return null;
        }
    }
}
