using Microsoft.Extensions.Logging;

namespace mobile
{
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow>? _logger;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                _logger = Handler?.MauiContext?.Services.GetService<ILogger<MainWindow>>();
            }
            catch { }

#if WINDOWS
            // Appliquer le thème aux boutons système après que la fenêtre soit créée
            this.HandlerChanged += OnHandlerChanged;
#endif
        }

#if WINDOWS
        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (Handler?.PlatformView is Microsoft.UI.Xaml.Window winUIWindow)
            {
                try
                {
                    Platforms.Windows.WindowsTitleBarHelper.ApplyTheme(winUIWindow);
                    _logger?.LogInformation("✅ Thème titlebar appliqué");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "❌ Erreur application thème titlebar");
                }
            }
        }
#endif

        /// <summary>
        /// Met à jour le bouton Account avec les infos utilisateur
        /// </summary>
        public void UpdateAccountButton(string firstName, string lastName)
        {
            // Générer les initiales
            var initials = GetInitials(firstName, lastName);
            AccountButton.Text = initials;
            AccountButton.IsVisible = true;
            if (this.FindByName<ImageButton>("PeopleButton") is ImageButton people)
            {
                people.IsVisible = false;
            }

            // Appliquer l'état authentifié sur tous les boutons du TitleBar
            SetTitleBarAuthState(true);

            _logger?.LogInformation("✅ Bouton Account affiché pour: {Name} ({Initials})", $"{firstName} {lastName}", initials);
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

        /// <summary>
        /// Cache le bouton Account (déconnexion)
        /// </summary>
        public void ClearAccountButton()
        {
            AccountButton.IsVisible = false;
            if (this.FindByName<ImageButton>("PeopleButton") is ImageButton people)
            {
                people.IsVisible = true;
            }
            // Appliquer l'état non authentifié sur tous les boutons du TitleBar
            SetTitleBarAuthState(false);
            _logger?.LogInformation("🧹 Bouton Account masqué");
        }

        /// <summary>
        /// Active/désactive et montre/masque les boutons du TitleBar selon l'authentification
        /// </summary>
        private void SetTitleBarAuthState(bool isAuthenticated)
        {
            try
            {
                // Boutons de droite (Messages, Notifications, Settings)
                var msg = this.FindByName<ImageButton>("MessagesButton");
                var noti = this.FindByName<ImageButton>("NotificationsButton");
                var set = this.FindByName<ImageButton>("SettingsButton");
                var search = this.FindByName<SearchBar>("TitleSearchBar");

                if (msg != null)
                {
                    msg.IsVisible = isAuthenticated;
                    msg.IsEnabled = isAuthenticated;
                }
                if (noti != null)
                {
                    noti.IsVisible = isAuthenticated;
                    noti.IsEnabled = isAuthenticated;
                }
                if (set != null)
                {
                    set.IsVisible = isAuthenticated;
                    set.IsEnabled = isAuthenticated;
                }

                if (search != null)
                {
                    search.IsVisible = isAuthenticated;
                    search.IsEnabled = isAuthenticated;
                    search.IsReadOnly = !isAuthenticated;
                }

                // Boutons de gauche gérés par UpdateAccountButton/ClearAccountButton
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur SetTitleBarAuthState");
            }
        }

        /// <summary>
        /// Appelé quand on clique sur le bouton People (non connecté)
        /// </summary>
        private async void OnPeopleTapped(object? sender, EventArgs e)
        {
            try
            {
                _logger?.LogInformation("👥 Bouton People cliqué (non connecté)");
                if (this.Page is Shell shell)
                {
                    await shell.GoToAsync("///login");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur lors du clic sur People");
            }
        }

        /// <summary>
        /// Appelé quand on clique sur le bouton Notifications
        /// </summary>
        private async void OnNotificationsTapped(object? sender, EventArgs e)
        {
            try
            {
                _logger?.LogInformation("🔔 Bouton Notifications cliqué");

                if (this.Page != null)
                {
                    await this.Page.DisplayAlert("Notifications", "Aucune nouvelle notification", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur lors du clic sur Notifications");
            }
        }

        // Alias pour correspondre à l'attribut XAML Clicked="NotificationsTapped"
        private async void NotificationsTapped(object? sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    _logger?.LogInformation("🔔 Bouton Notifications cliqué (alias)");
                    if (this.Page != null)
                    {
                        await this.Page.DisplayAlert("Notifications", "Aucune nouvelle notification", "OK");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "❌ Erreur lors du clic sur Notifications (alias)");
                }
            });
        }

        /// <summary>
        /// Appelé quand on clique sur le bouton Messages
        /// </summary>
        private async void OnMessagesTapped(object? sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    _logger?.LogInformation("💬 Bouton Messages cliqué");
                    if (this.Page != null)
                    {
                        await this.Page.DisplayAlert("Messages", "Aucun nouveau message", "OK");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "❌ Erreur lors du clic sur Messages");
                }
            });
        }

        /// <summary>
        /// Appelé quand on clique sur le bouton Settings
        /// </summary>
        private async void OnSettingsTapped(object? sender, EventArgs e)
        {
            try
            {
                _logger?.LogInformation("⚙️ Bouton Settings cliqué");

                if (this.Page != null)
                {
                    await this.Page.DisplayAlert("Paramètres", "Page de paramètres (à implémenter)", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur lors du clic sur Settings");
            }
        }

        /// <summary>
        /// Appelé quand on clique sur le bouton Account
        /// </summary>
        private async void OnAccountTapped(object? sender, EventArgs e)
        {
            try
            {
                _logger?.LogInformation("👤 Bouton Account cliqué");

                if (this.Page != null)
                {
                    await this.Page.DisplayAlert("Account", "Page de Account (à implémenter)", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur lors du clic sur Account");
            }
        }
    }
}
