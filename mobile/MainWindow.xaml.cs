using mobile.Services.Stores;
using mobile.Services.Theme;

namespace mobile
{
    public partial class MainWindow : Window
    {
        private IThemeService _themeService;
        private INotificationStore _notificationStore;
        private IConversationStore _conversationStore;

        private NotificationCenterPage? notificationCenterPage;
        private MessageCenterPage? messageCenterPage;
        private ParameterCenterPage? parameterCenterPage;

        public MainWindow (IThemeService themeService, INotificationStore notificationStore, IConversationStore conversationStore)
        {
            InitializeComponent();

            _themeService = themeService;
            _notificationStore = notificationStore;
            _conversationStore = conversationStore;

            try
            {
                // S'abonner aux changements de thème
                if (_themeService != null)
                {
                    _themeService.ThemeChanged += OnThemeChanged;
                }

                // S'abonner aux changements du store de notifications
                if (_notificationStore != null)
                {
                    _notificationStore.PropertyChanged += OnNotificationStoreChanged;
                    UpdateNotificationBadge();
                }

                // S'abonner aux changements du store de conversations
                if (_conversationStore != null)
                {
                    _conversationStore.PropertyChanged += OnConversationStoreChanged;
                    UpdateMessageBadge();
                }
            }
            catch { }

            // Appliquer le thème aux boutons système après que la fenêtre soit créée
            this.HandlerChanged += OnHandlerChanged;
        }

        private void OnHandlerChanged (object? sender, EventArgs e)
        {
#if WINDOWS
            if (Handler?.PlatformView is Microsoft.UI.Xaml.Window winUIWindow)
            {
                try
                {
                    Platforms.Windows.WindowsTitleBarHelper.ApplyTheme(winUIWindow);
                    // Thème titlebar appliqué
                }
                catch (Exception ex)
                {
#if DEBUG
                    Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur application thème titlebar: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                }
            }
#endif
        }

        private async void OnThemeChanged (object? sender, AppTheme newTheme)
        {
#if WINDOWS
            if (Handler?.PlatformView is Microsoft.UI.Xaml.Window winUIWindow)
            {
                try
                {
                    // Petit délai pour s'assurer que les ressources sont mises à jour
                    await Task.Delay(100);
                    Platforms.Windows.WindowsTitleBarHelper.ApplyTheme(winUIWindow);
                    // Thème titlebar mis à jour
                }
                catch (Exception ex)
                {
#if DEBUG
                    await Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur mise à jour thème titlebar: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                }
            }
#endif
        }

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

            // Bouton Account affiché
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
            // Bouton Account masqué
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
                // var search = this.FindByName<SearchBar>("TitleSearchBar");
                // if (search != null)
                // {
                //     search.IsVisible = false;
                // }

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
                // Éléments de la title bar masqués (splash screen)
            }
            catch (Exception ex)
            {
#if DEBUG
                Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur lors du masquage des éléments de la title bar: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                // var search = this.FindByName<SearchBar>("TitleSearchBar");
                // if (search != null)
                // {
                //     search.IsVisible = true;
                // }

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

                // Éléments de la title bar affichés
            }
            catch (Exception ex)
            {
#if DEBUG
                Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur lors de l'affichage des éléments de la title bar: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                // var search = this.FindByName<SearchBar>("TitleSearchBar");

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

                //if (search != null)
                //{
                //    search.IsVisible = isAuthenticated;
                //    search.IsEnabled = isAuthenticated;
                //    search.IsReadOnly = !isAuthenticated;
                //}

                // Boutons de gauche gérés par UpdateAccountButton/ClearAccountButton
            }
            catch (Exception ex)
            {
#if DEBUG
                Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur SetTitleBarAuthState: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        /// <summary>
        /// Appelé quand on clique sur le bouton People (non connecté)
        /// </summary>
        private async void OnPeopleTapped (object? sender, EventArgs e)
        {
            try
            {
                // Bouton People cliqué (non connecté)
                if (this.Page is Shell shell)
                {
                    await shell.GoToAsync("///login");
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur lors du clic sur People: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        // Alias pour correspondre à l'attribut XAML Clicked="NotificationsTapped"
        private async void NotificationsTapped (object? sender, EventArgs e)
        {
            try
            {
                // Bouton Notifications cliqué

                // Ouvrir le centre de notifications en modal
                if (this.Page != null)
                {
                    if (messageCenterPage != null)
                    {
                        await this.Page.Navigation.PopModalAsync(animated: true);
                        messageCenterPage = null;
                    }
                    if (parameterCenterPage != null)
                    {
                        await this.Page.Navigation.PopModalAsync(animated: true);
                        parameterCenterPage = null;
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
#if DEBUG
                await Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur lors de l'affichage du centre de notifications: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                        // Mise à jour badge notifications

                        // Trouver les éléments par nom si pas encore initialisés
                        var badge = NotificationBadge ?? this.FindByName<Border>("NotificationBadge");
                        var badgeText = NotificationBadgeText ?? this.FindByName<Label>("NotificationBadgeText");

                        if (badge != null && badgeText != null)
                        {
                            badge.IsVisible = unreadCount > 0;
                            badgeText.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                            // Badge mis à jour
                        }
                        else
                        {
                            // Badge ou BadgeText introuvable
                        }
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur lors de la mise à jour du badge notifications: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                    // Bouton Messages cliqué
                    // Ouvrir le centre de messages en modal
                    if (this.Page != null)
                    {
                        if (notificationCenterPage != null)
                        {
                            await this.Page.Navigation.PopModalAsync(animated: true);
                            notificationCenterPage = null;
                        }
                        if (parameterCenterPage == null)
                        {
                            await this.Page.Navigation.PopModalAsync(animated: true);
                            parameterCenterPage = null;
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
#if DEBUG
                    await Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur lors du clic sur Messages: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                }
            });
        }


        /// <summary>
        /// Appelé quand le store de conversations change
        /// </summary>
        private void OnConversationStoreChanged (object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IConversationStore.TotalUnreadCount))
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
                    if (_conversationStore != null)
                    {
                        var unreadCount = _conversationStore.TotalUnreadCount;
                        // Mise à jour badge messages

                        // Trouver les éléments par nom si pas encore initialisés
                        var badge = MessageBadge ?? this.FindByName<Border>("MessageBadge");
                        var badgeText = MessageBadgeText ?? this.FindByName<Label>("MessageBadgeText");

                        if (badge != null && badgeText != null)
                        {
                            badge.IsVisible = unreadCount > 0;
                            badgeText.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                            // Badge mis à jour
                        }
                        else
                        {
                            // Badge ou BadgeText introuvable
                        }
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur lors de la mise à jour du badge messages: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                // Bouton Settings cliqué

                if (this.Page != null)
                {
                    // Vérifier si un modal ParameterCenterPage est déjà ouvert (depuis ProfilePage par exemple)
                    var modalStack = this.Page.Navigation.ModalStack;
                    var existingParameterPage = modalStack.FirstOrDefault(p => p is ParameterCenterPage);

                    if (existingParameterPage != null && parameterCenterPage == null)
                    {
                        // Un modal est déjà ouvert depuis ailleurs (ProfilePage), le fermer
                        // ParameterCenterPage déjà ouvert depuis ailleurs, fermeture
                        await this.Page.Navigation.PopModalAsync(animated: true);
                        return;
                    }

                    // Fermer les autres centres s'ils sont ouverts
                    if (notificationCenterPage != null)
                    {
                        await this.Page.Navigation.PopModalAsync(animated: true);
                        notificationCenterPage = null;
                    }
                    if (messageCenterPage != null)
                    {
                        await this.Page.Navigation.PopModalAsync(animated: true);
                        messageCenterPage = null;
                    }

                    // Toggle le centre de paramètres
                    if (parameterCenterPage == null)
                    {
                        parameterCenterPage = new ParameterCenterPage();
                        await this.Page.Navigation.PushModalAsync(parameterCenterPage, animated: true);
                    }
                    else
                    {
                        await this.Page.Navigation.PopModalAsync(animated: true);
                        parameterCenterPage = null;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur lors du clic sur Settings: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                // Bouton Account cliqué - Navigation vers ProfilePage

                if (this.Page is Shell shell)
                {
                    // Fermer le flyout s'il est ouvert
                    shell.FlyoutIsPresented = false;

                    // Naviguer vers la page de profil
                    await shell.GoToAsync("///profile");

                    // Navigation vers ProfilePage réussie
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug MainWindow", $"❌ Erreur lors de la navigation vers ProfilePage: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        /// <summary>
        /// Nettoyage lors de la destruction de la fenêtre
        /// </summary>
        protected override void OnHandlerChanging (HandlerChangingEventArgs args)
        {
            base.OnHandlerChanging(args);

            // Se désabonner de l'événement ThemeChanged
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            }
        }
    }
}
