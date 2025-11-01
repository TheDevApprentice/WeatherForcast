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
            // Générer les initiales
            var initials = GetInitials(firstName, lastName);
            AccountButton.Text = initials;
            AccountButton.IsVisible = true;

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
