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

            // Connexion toujours masquée du flyout
            var loginItem = Items.FirstOrDefault(i => i.Route == "login");
            if (loginItem != null)
            {
                loginItem.FlyoutItemIsVisible = false;
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
