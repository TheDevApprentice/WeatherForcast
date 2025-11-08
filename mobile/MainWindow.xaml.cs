using Microsoft.Extensions.Logging;
using mobile.Services.Theme;

namespace mobile
{
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow>? _logger;
        private IThemeService? _themeService;
        private INotificationStore? _notificationStore;
        private IMessageStore? _messageStore;

        private NotificationCenterPage? notificationCenterPage;
        private MessageCenterPage? messageCenterPage;

        public MainWindow ()
        {
            InitializeComponent();

            try
            {
                _logger = Handler?.MauiContext?.Services.GetService<ILogger<MainWindow>>();
                _themeService = Handler?.MauiContext?.Services.GetService<IThemeService>();
                _notificationStore = Handler?.MauiContext?.Services.GetService<INotificationStore>();
                _messageStore = Handler?.MauiContext?.Services.GetService<IMessageStore>();

#if WINDOWS
                // S'abonner aux changements de thème
                if (_themeService != null)
                {
                    _themeService.ThemeChanged += OnThemeChanged;
                }
#endif

                // S'abonner aux changements du store de notifications
                if (_notificationStore != null)
                {
                    _notificationStore.PropertyChanged += OnNotificationStoreChanged;
                    UpdateNotificationBadge();
                }

                // S'abonner aux changements du store de messages
                if (_messageStore != null)
                {

                }
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

        private async void OnThemeChanged(object? sender, AppTheme newTheme)
        {
            if (Handler?.PlatformView is Microsoft.UI.Xaml.Window winUIWindow)
            {
                try
                {
                    // Petit délai pour s'assurer que les ressources sont mises à jour
                    await Task.Delay(100);
                    Platforms.Windows.WindowsTitleBarHelper.ApplyTheme(winUIWindow);
                    _logger?.LogInformation("✅ Thème titlebar mis à jour: {Theme}", newTheme);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "❌ Erreur mise à jour thème titlebar");
                }
            }
        }
#endif

        /// <summary>
        /// Met à jour le bouton Account avec les infos utilisateur
        /// </summary>
        public void UpdateAccountButton (string firstName, string lastName)
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
        private string GetInitials (string firstName, string lastName)
        {
            var firstInitial = !string.IsNullOrEmpty(firstName) ? firstName[0].ToString().ToUpper() : "";
            var lastInitial = !string.IsNullOrEmpty(lastName) ? lastName[0].ToString().ToUpper() : "";
            return firstInitial + lastInitial;
        }

        /// <summary>
        /// Cache le bouton Account (déconnexion)
        /// </summary>
        public void ClearAccountButton ()
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
        /// Masque tous les éléments de la title bar sauf l'icône account, le titre et le sous-titre
        /// Utilisé pendant le splash screen
        /// </summary>
        public void HideTitleBarElements ()
        {
            try
            {
                // Masquer la SearchBar
                var search = this.FindByName<SearchBar>("TitleSearchBar");
                if (search != null)
                {
                    search.IsVisible = false;
                }

                // Masquer les boutons de droite
                var msg = this.FindByName<ImageButton>("MessagesButton");
                var noti = this.FindByName<ImageButton>("NotificationsButton");
                var set = this.FindByName<ImageButton>("SettingsButton");
                var msgBdg = this.FindByName<Border>("MessageBadge");
                var notiBdg = this.FindByName<Border>("NotificationBadge");
                if (msg != null) msg.IsVisible = false;
                if (msgBdg != null) msgBdg.IsVisible = false;
                if (noti != null) noti.IsVisible = false;
                if (notiBdg != null) notiBdg.IsVisible = false;
                if (set != null) set.IsVisible = false;

                // Masquer les boutons Account et People
                AccountButton.IsVisible = false;
                if (this.FindByName<ImageButton>("PeopleButton") is ImageButton people)
                {
                    people.IsVisible = false;
                }

                _logger?.LogInformation("🔒 Éléments de la title bar masqués (splash screen)");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur lors du masquage des éléments de la title bar");
            }
        }

        /// <summary>
        /// Affiche les éléments de la title bar après le splash screen
        /// </summary>
        public void ShowTitleBarElements (bool isAuthenticated)
        {
            try
            {
                // Afficher la SearchBar
                var search = this.FindByName<SearchBar>("TitleSearchBar");
                if (search != null)
                {
                    search.IsVisible = true;
                }

                // Afficher les boutons selon l'état d'authentification
                SetTitleBarAuthState(isAuthenticated);

                // Afficher le bon bouton (Account ou People)
                if (isAuthenticated)
                {
                    AccountButton.IsVisible = true;
                    if (this.FindByName<ImageButton>("PeopleButton") is ImageButton people)
                    {
                        people.IsVisible = false;
                    }
                }
                else
                {
                    AccountButton.IsVisible = false;
                    if (this.FindByName<ImageButton>("PeopleButton") is ImageButton people)
                    {
                        people.IsVisible = true;
                    }
                }

                _logger?.LogInformation("✅ Éléments de la title bar affichés (auth: {IsAuth})", isAuthenticated);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur lors de l'affichage des éléments de la title bar");
            }
        }

        /// <summary>
        /// Active/désactive et montre/masque les boutons du TitleBar selon l'authentification
        /// </summary>
        private void SetTitleBarAuthState (bool isAuthenticated)
        {
            try
            {
                // Boutons de droite (Messages, Notifications, Settings)
                var msg = this.FindByName<ImageButton>("MessagesButton");
                var msgBdg = this.FindByName<Border>("MessageBadge");
                var noti = this.FindByName<ImageButton>("NotificationsButton");
                var notiBdg = this.FindByName<Border>("NotificationBadge");
                var set = this.FindByName<ImageButton>("SettingsButton");
                var search = this.FindByName<SearchBar>("TitleSearchBar");

                if (msg != null)
                {
                    msg.IsVisible = isAuthenticated;
                    msg.IsEnabled = isAuthenticated;
                }
                if (msgBdg != null)
                {
                    msgBdg.IsVisible = isAuthenticated;
                    msgBdg.IsEnabled = isAuthenticated;
                }
                if (noti != null)
                {
                    noti.IsVisible = isAuthenticated;
                    noti.IsEnabled = isAuthenticated;
                }
                if (notiBdg != null)
                {
                    notiBdg.IsVisible = isAuthenticated;
                    notiBdg.IsEnabled = isAuthenticated;
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
        private async void OnPeopleTapped (object? sender, EventArgs e)
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

        // Alias pour correspondre à l'attribut XAML Clicked="NotificationsTapped"
        private async void NotificationsTapped (object? sender, EventArgs e)
        {
            try
            {
                _logger?.LogInformation("🔔 Bouton Notifications cliqué");

                // Ouvrir le centre de notifications en modal
                if (this.Page != null)
                {
                    if (messageCenterPage != null)
                    {
                        await this.Page.Navigation.PopModalAsync(animated: true);
                        messageCenterPage = null;
                    }
                    if (notificationCenterPage == null)
                    {
                        notificationCenterPage = new NotificationCenterPage();
                        await this.Page.Navigation.PushModalAsync(notificationCenterPage, animated: true);
                    }
                    else
                    {
                        await this.Page.Navigation.PopModalAsync(animated: true);
                        notificationCenterPage = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur lors de l'affichage du centre de notifications");
            }
        }

        /// <summary>
        /// Appelé quand le store de notifications change
        /// </summary>
        private void OnNotificationStoreChanged (object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(INotificationStore.UnreadCount))
            {
                UpdateNotificationBadge();
            }
        }

        /// <summary>
        /// Met à jour le badge de compteur de notifications
        /// </summary>
        private void UpdateNotificationBadge ()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_notificationStore != null)
                    {
                        var unreadCount = _notificationStore.UnreadCount;
                        _logger?.LogInformation("🔔 Mise à jour badge: {Count} notifications non lues", unreadCount);

                        // Trouver les éléments par nom si pas encore initialisés
                        var badge = NotificationBadge ?? this.FindByName<Border>("NotificationBadge");
                        var badgeText = NotificationBadgeText ?? this.FindByName<Label>("NotificationBadgeText");

                        if (badge != null && badgeText != null)
                        {
                            badge.IsVisible = unreadCount > 0;
                            badgeText.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                            _logger?.LogInformation("✅ Badge mis à jour: visible={Visible}, text={Text}", badge.IsVisible, badgeText.Text);
                        }
                        else
                        {
                            _logger?.LogWarning("⚠️ Badge ou BadgeText introuvable");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "❌ Erreur lors de la mise à jour du badge");
                }
            });
        }

        /// <summary>
        /// Appelé quand on clique sur le bouton Messages
        /// </summary>
        private async void OnMessagesTapped (object? sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    _logger?.LogInformation("💬 Bouton Messages cliqué");
                    // Ouvrir le centre de messages en modal
                    if (this.Page != null)
                    {
                        if (notificationCenterPage != null)
                        {
                            await this.Page.Navigation.PopModalAsync(animated: true);
                            notificationCenterPage = null;
                        }
                        if (messageCenterPage == null)
                        {
                            messageCenterPage = new MessageCenterPage();
                            await this.Page.Navigation.PushModalAsync(messageCenterPage, animated: true);
                        }
                        else
                        {
                            await this.Page.Navigation.PopModalAsync(animated: true);
                            messageCenterPage = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "❌ Erreur lors du clic sur Messages");
                }
            });
        }


        /// <summary>
        /// Appelé quand le store de messsage change
        /// </summary>
        private void OnMessageStoreChanged (object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMessageStore.UnreadCount))
            {
                UpdateMessageBadge();
            }
        }

        /// <summary>
        /// Met à jour le badge de compteur de messsages
        /// </summary>
        private void UpdateMessageBadge ()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (_messageStore != null)
                    {
                        var unreadCount = _messageStore.UnreadCount;
                        _logger?.LogInformation("🔔 Mise à jour badge: {Count} messages non lues", unreadCount);

                        // Trouver les éléments par nom si pas encore initialisés
                        var badge = MessageBadge ?? this.FindByName<Border>("MessageBadge");
                        var badgeText = MessageBadgeText ?? this.FindByName<Label>("MessageBadgeText");

                        if (badge != null && badgeText != null)
                        {
                            badge.IsVisible = unreadCount > 0;
                            badgeText.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                            _logger?.LogInformation("✅ Badge mis à jour: visible={Visible}, text={Text}", badge.IsVisible, badgeText.Text);
                        }
                        else
                        {
                            _logger?.LogWarning("⚠️ Badge ou BadgeText introuvable");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "❌ Erreur lors de la mise à jour du badge");
                }
            });
        }


        /// <summary>
        /// Appelé quand on clique sur le bouton Settings
        /// </summary>
        private async void OnSettingsTapped (object? sender, EventArgs e)
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
        /// Navigue vers la page de profil
        /// </summary>
        private async void OnAccountTapped (object? sender, EventArgs e)
        {
            try
            {
                _logger?.LogInformation("👤 Bouton Account cliqué - Navigation vers ProfilePage");

                if (this.Page is Shell shell)
                {
                    // Fermer le flyout s'il est ouvert
                    shell.FlyoutIsPresented = false;

                    // Naviguer vers la page de profil
                    await shell.GoToAsync("///profile");

                    _logger?.LogInformation("✅ Navigation vers ProfilePage réussie");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur lors de la navigation vers ProfilePage");
            }
        }

        /// <summary>
        /// Nettoyage lors de la destruction de la fenêtre
        /// </summary>
        protected override void OnHandlerChanging (HandlerChangingEventArgs args)
        {
            base.OnHandlerChanging(args);

#if WINDOWS
            // Se désabonner de l'événement ThemeChanged
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            }
#endif
        }
    }
}
