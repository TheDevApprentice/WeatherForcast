using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Font = Microsoft.Maui.Font;

namespace mobile
{
    public partial class AppShell : Shell
    {
        public AppShell ()
        {
            InitializeComponent();
            var currentTheme = Application.Current!.RequestedTheme;
            // Initialize the new Switch based on current theme
            ThemeSwitch.IsToggled = currentTheme == AppTheme.Dark;

            // Enregistrer les routes pour la navigation
            Routing.RegisterRoute("register", typeof(RegisterPage));

            // Écouter l'ouverture/fermeture du flyout pour animer
            this.PropertyChanged += OnShellPropertyChanged;
        }

        /// <summary>
        /// Détecte quand le flyout s'ouvre pour déclencher les animations
        /// </summary>
        private void OnShellPropertyChanged (object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FlyoutIsPresented) && FlyoutIsPresented)
            {
                // Le flyout vient de s'ouvrir, démarrer les animations
                _ = AnimateFlyoutOpen();
            }
        }

        /// <summary>
        /// Anime l'apparition des éléments du flyout
        /// </summary>
        private async Task AnimateFlyoutOpen ()
        {
            try
            {
                // Les éléments sont déjà initialisés à Opacity=0 dans le XAML
                // Pas besoin de les réinitialiser ici

                // Petit délai pour s'assurer que le flyout est bien visible
                await Task.Delay(50);

                // Animation 1 : Avatar apparait avec scale + fade (effet pop)
                await Task.WhenAll(
                    UserInitialsLabel.FadeTo(1, 400, Easing.CubicOut),
                    UserInitialsLabel.ScaleTo(1, 400, Easing.SpringOut)
                );

                // Animation 2 : Nom glisse de gauche + fade
                await Task.WhenAll(
                    UserFullNameLabel.FadeTo(1, 300, Easing.CubicOut),
                    UserFullNameLabel.TranslateTo(0, 0, 300, Easing.CubicOut)
                );

                // Animation 3 : Email glisse de gauche + fade
                await Task.WhenAll(
                    UserEmailLabel.FadeTo(1, 300, Easing.CubicOut),
                    UserEmailLabel.TranslateTo(0, 0, 300, Easing.CubicOut)
                );
            }
            catch
            {
                // Si erreur, remettre tout visible
                UserInitialsLabel.Opacity = 1;
                UserInitialsLabel.Scale = 1;
                UserFullNameLabel.Opacity = 1;
                UserFullNameLabel.TranslationX = 0;
                UserEmailLabel.Opacity = 1;
                UserEmailLabel.TranslationX = 0;
            }
        }

        /// <summary>
        /// Met à jour la visibilité du menu selon l'état d'authentification
        /// </summary>
        /// <param name="isAuthenticated">État d'authentification</param>
        /// <param name="updateTitleBar">Si false, ne met pas à jour la title bar (utilisé pendant le splash)</param>
        public void UpdateAuthenticationUI (bool isAuthenticated, bool updateTitleBar = true)
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
                var authStateService = Handler?.MauiContext?.Services.GetService<IAuthenticationStateService>();
                if (authStateService != null)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var authState = await authStateService.GetStateAsync();

                        if (authState.IsAuthenticated)
                        {
                            UserFullNameLabel.Text = authState.GetFullName();
                            UserEmailLabel.Text = authState.Email;
                            UserInitialsLabel.Text = authState.GetInitials();

#if WINDOWS || MACCATALYST
                            // Synchroniser avec la titlebar (sauf si on est dans le splash)
                            if (updateTitleBar && Application.Current?.Windows?.Count > 0 && Application.Current.Windows[0] is MainWindow mw)
                            {
                                mw.UpdateAccountButton(authState.FirstName, authState.LastName);
                            }
#endif
                        }
                    });
                }
            }
            else
            {
                UserFullNameLabel.Text = string.Empty;
                UserEmailLabel.Text = string.Empty;
                UserInitialsLabel.Text = string.Empty;

#if WINDOWS || MACCATALYST
                // Masquer le bouton Account et afficher l'icône People dans la titlebar (sauf si on est dans le splash)
                if (updateTitleBar && Application.Current?.Windows?.Count > 0 && Application.Current.Windows[0] is MainWindow mw)
                {
                    mw.ClearAccountButton();
                }
#endif
            }
        }

        private async void OnLogoutClicked (object sender, EventArgs e)
        {
            try
            {
                // Récupérer les services
                var secureStorage = Handler?.MauiContext?.Services.GetService<ISecureStorageService>();
                var authStateService = Handler?.MauiContext?.Services.GetService<IAuthenticationStateService>();
                var apiAuthService = Handler?.MauiContext?.Services.GetService<IApiAuthService>();

                if (secureStorage != null && authStateService != null && apiAuthService != null)
                {
                    // Appeler l'API pour déconnecter
                    await apiAuthService.LogoutAsync();

                    // Supprimer les données locales
                    await secureStorage.ClearAllAsync();

                    // Effacer l'état d'authentification centralisé
                    await authStateService.ClearStateAsync();

                    // Mettre à jour l'UI
                    UpdateAuthenticationUI(false);

                    // Rediriger vers la page de connexion
                    await Shell.Current.GoToAsync("///login");

                    //await DisplayToastAsync("Déconnexion réussie");
                }
            }
            catch (Exception ex)
            {
                await DisplaySnackbarAsync($"Erreur lors de la déconnexion: {ex.Message}");
            }
        }

        protected override void OnNavigated (ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);

            // Ne pas mettre à jour la title bar si on est sur le splash
            bool isOnSplash = args.Current?.Location?.OriginalString?.Contains("splash") ?? false;

            // Mettre à jour l'UI à chaque navigation (utilise l'état centralisé)
            var authStateService = Handler?.MauiContext?.Services.GetService<IAuthenticationStateService>();
            if (authStateService != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var isAuthenticated = await authStateService.IsAuthenticatedAsync();
                    // Ne pas mettre à jour la title bar pendant le splash
                    UpdateAuthenticationUI(isAuthenticated, updateTitleBar: !isOnSplash);
                });
            }
        }
        public static async Task DisplaySnackbarAsync (string message)
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

        public static async Task DisplayToastAsync (string message)
        {
            // Toast is currently not working in MCT on Windows
            if (OperatingSystem.IsWindows())
                return;

            var toast = Toast.Make(message, textSize: 18);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await toast.Show(cts.Token);
        }

        private void ThemeSwitch_Toggled (object sender, ToggledEventArgs e)
        {
            Application.Current!.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
        }
    }
}
