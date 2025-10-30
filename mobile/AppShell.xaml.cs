using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using mobile.Pages.Auth;
using mobile.Services;
using Font = Microsoft.Maui.Font;

namespace mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            var currentTheme = Application.Current!.RequestedTheme;
            ThemeSegmentedControl.SelectedIndex = currentTheme == AppTheme.Light ? 0 : 1;

            // Enregistrer les routes pour la navigation
            Routing.RegisterRoute("register", typeof(RegisterPage));
        }

        /// <summary>
        /// Met à jour la visibilité du menu selon l'état d'authentification
        /// </summary>
        public void UpdateAuthenticationUI(bool isAuthenticated)
        {
            // Contrôler la visibilité du Flyout
            FlyoutBehavior = isAuthenticated ? FlyoutBehavior.Flyout : FlyoutBehavior.Disabled;

            // Contrôler la visibilité des items du menu
            // Dashboard visible seulement si connecté
            var dashboardItem = Items.FirstOrDefault(i => i.Route == "main");
            if (dashboardItem != null)
            {
                dashboardItem.IsVisible = isAuthenticated;
            }

            // Prévisions visibles seulement si connecté
            var forecastsItem = Items.FirstOrDefault(i => i.Route == "forecasts");
            if (forecastsItem != null)
            {
                forecastsItem.IsVisible = isAuthenticated;
            }

            // Connexion toujours masquée du flyout
            var loginItem = Items.FirstOrDefault(i => i.Route == "login");
            if (loginItem != null)
            {
                loginItem.FlyoutItemIsVisible = false;
            }

            // Bouton de déconnexion visible seulement si connecté
            LogoutButton.IsVisible = isAuthenticated;

            // Mettre à jour les informations utilisateur dans le header
            if (isAuthenticated)
            {
                var secureStorage = Handler?.MauiContext?.Services.GetService<ISecureStorageService>();
                if (secureStorage != null)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var userInfo = await secureStorage.GetUserInfoAsync();
                        UserFullNameLabel.Text = $"{userInfo.FirstName} {userInfo.LastName}";
                        UserEmailLabel.Text = userInfo.Email;
                        
                        // Générer les initiales (première lettre du prénom + première lettre du nom)
                        var initials = GetInitials(userInfo.FirstName, userInfo.LastName);
                        UserInitialsLabel.Text = initials;
                    });
                }
            }
            else
            {
                UserFullNameLabel.Text = string.Empty;
                UserEmailLabel.Text = string.Empty;
                UserInitialsLabel.Text = string.Empty;
            }
        }

        /// <summary>
        /// Génère les initiales à partir du prénom et du nom
        /// </summary>
        private string GetInitials(string firstName, string lastName)
        {
            var firstInitial = !string.IsNullOrEmpty(firstName) ? firstName[0].ToString().ToUpper() : "";
            var lastInitial = !string.IsNullOrEmpty(lastName) ? lastName[0].ToString().ToUpper() : "";
            return firstInitial + lastInitial;
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                // Récupérer les services
                var secureStorage = Handler?.MauiContext?.Services.GetService<ISecureStorageService>();
                var apiService = Handler?.MauiContext?.Services.GetService<IApiService>();

                if (secureStorage != null && apiService != null)
                {
                    // Appeler l'API pour déconnecter
                    await apiService.LogoutAsync();

                    // Supprimer les données locales
                    await secureStorage.ClearAllAsync();

                    // Mettre à jour l'UI
                    UpdateAuthenticationUI(false);

                    // Rediriger vers la page de connexion
                    await Shell.Current.GoToAsync("///login");

                    await DisplayToastAsync("Déconnexion réussie");
                }
            }
            catch (Exception ex)
            {
                await DisplaySnackbarAsync($"Erreur lors de la déconnexion: {ex.Message}");
            }
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);
            
            // Mettre à jour l'UI à chaque navigation
            var secureStorage = Handler?.MauiContext?.Services.GetService<ISecureStorageService>();
            if (secureStorage != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var isAuthenticated = await secureStorage.IsAuthenticatedAsync();
                    UpdateAuthenticationUI(isAuthenticated);
                });
            }
        }
        public static async Task DisplaySnackbarAsync(string message)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            var snackbarOptions = new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#FF3300"),
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.Yellow,
                CornerRadius = new CornerRadius(0),
                Font = Font.SystemFontOfSize(18),
                ActionButtonFont = Font.SystemFontOfSize(14)
            };

            var snackbar = Snackbar.Make(message, visualOptions: snackbarOptions);

            await snackbar.Show(cancellationTokenSource.Token);
        }

        public static async Task DisplayToastAsync(string message)
        {
            // Toast is currently not working in MCT on Windows
            if (OperatingSystem.IsWindows())
                return;

            var toast = Toast.Make(message, textSize: 18);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await toast.Show(cts.Token);
        }

        private void SfSegmentedControl_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
        {
            Application.Current!.UserAppTheme = e.NewIndex == 0 ? AppTheme.Light : AppTheme.Dark;
        }
    }
}
