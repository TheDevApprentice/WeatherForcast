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
        }

        /// <summary>
        /// Met à jour le bouton Account avec les infos utilisateur
        /// </summary>
        public void UpdateAccountButton(string firstName, string lastName)
        {
            AccountButton.IsVisible = true;
            _logger?.LogInformation("✅ Bouton Account affiché pour: {Name}", $"{firstName} {lastName}");
        }

        /// <summary>
        /// Cache le bouton Account (déconnexion)
        /// </summary>
        public void ClearAccountButton()
        {
            AccountButton.IsVisible = false;
            _logger?.LogInformation("🧹 Bouton Account masqué");
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
                    // var action = await this.Page.DisplayActionSheet(
                    //     "Mon Compte",
                    //     "Annuler",
                    //     null,
                    //     "👤 Profil",
                    //     "⚙️ Paramètres",
                    //     "🚪 Déconnexion"
                    // );

                    // _logger?.LogInformation("Action sélectionnée: {Action}", action);

                    // switch (action)
                    // {
                    //     case "👤 Profil":
                    //         await this.Page.DisplayAlert("Profil", "Page de profil (à implémenter)", "OK");
                    //         break;

                    //     case "⚙️ Paramètres":
                    //         await this.Page.DisplayAlert("Paramètres", "Page de paramètres (à implémenter)", "OK");
                    //         break;

                    //     case "🚪 Déconnexion":
                    //         // TODO: Appeler la méthode de déconnexion
                    //         await this.Page.DisplayAlert("Déconnexion", "Déconnexion (à implémenter)", "OK");
                    //         break;
                    // }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur lors du clic sur Account");
            }
        }
    }
}
