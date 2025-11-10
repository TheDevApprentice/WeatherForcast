using Microsoft.Extensions.Logging;
using mobile.Resources.Styles;

namespace mobile.Services.Theme
{
    /// <summary>
    /// Service centralisé pour la gestion des thèmes de l'application
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Thème actuellement actif
        /// </summary>
        AppTheme CurrentTheme { get; }

        /// <summary>
        /// Événement déclenché lors du changement de thème
        /// </summary>
        event EventHandler<AppTheme>? ThemeChanged;

        /// <summary>
        /// Change le thème de l'application avec animation optionnelle
        /// </summary>
        /// <param name="theme">Le nouveau thème à appliquer</param>
        /// <param name="animated">Si true, applique une transition animée</param>
        Task SetThemeAsync (AppTheme theme, bool animated = true);

        /// <summary>
        /// Initialise le service avec l'overlay global pour les animations
        /// </summary>
        /// <param name="overlay">BoxView utilisé pour l'animation de transition</param>
        void RegisterGlobalOverlay (BoxView overlay);
    }

    /// <summary>
    /// Implémentation du service de gestion des thèmes avec animations
    /// </summary>
    public class ThemeService : IThemeService
    {
        private readonly ILogger<ThemeService> _logger;
        private BoxView? _globalOverlay;
        private bool _isTransitioning;

        public AppTheme CurrentTheme => Application.Current?.UserAppTheme ?? AppTheme.Unspecified;

        public event EventHandler<AppTheme>? ThemeChanged;

        public ThemeService (ILogger<ThemeService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Enregistre l'overlay global pour les animations de transition
        /// </summary>
        public void RegisterGlobalOverlay (BoxView overlay)
        {
            _globalOverlay = overlay;
            _logger.LogInformation("✅ Overlay global enregistré pour les transitions de thème");
        }

        /// <summary>
        /// Change le thème de l'application avec animation optionnelle
        /// </summary>
        public async Task SetThemeAsync (AppTheme theme, bool animated = true)
        {
            if (Application.Current == null)
            {
                _logger.LogWarning("⚠️ Application.Current est null");
                return;
            }

            if (CurrentTheme == theme)
            {
                _logger.LogDebug("ℹ️ Thème déjà actif: {Theme}", theme);
                return;
            }

            // Éviter les transitions multiples simultanées
            if (_isTransitioning)
            {
                _logger.LogDebug("⏳ Transition déjà en cours, ignorée");
                return;
            }

            try
            {
                _isTransitioning = true;

                if (animated && _globalOverlay != null)
                {
                    await AnimateThemeTransitionAsync(theme);
                }
                else
                {
                    ApplyTheme(theme);
                }

                _logger.LogInformation("✅ Thème changé: {Theme}", theme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du changement de thème");
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// Anime la transition entre les thèmes avec un overlay
        /// </summary>
        private async Task AnimateThemeTransitionAsync (AppTheme theme)
        {
            // Créer l'overlay à la volée s'il n'existe pas
            if (_globalOverlay == null)
            {
                _logger.LogDebug("Pas d'overlay global, transition sans animation");
                ApplyTheme(theme);
                return;
            }

            try
            {
                // Définir la couleur de l'overlay selon le thème cible
                _globalOverlay.BackgroundColor = theme == AppTheme.Dark
                    ? Color.FromArgb("#1C1C1E")
                    : Colors.White;

                // Phase 1 : Fondu vers la couleur (masque l'ancien thème)
                await _globalOverlay.FadeTo(1, 400, Easing.SinIn);

                // Phase 2 : Appliquer le nouveau thème pendant que l'overlay est opaque
                ApplyTheme(theme);

                // Petit délai pour s'assurer que le thème est appliqué
                await Task.Delay(50);

                // Phase 3 : Fondu de disparition (révèle le nouveau thème)
                await _globalOverlay.FadeTo(0, 450, Easing.SinOut);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'animation de transition");
                // En cas d'erreur, s'assurer que l'overlay est invisible
                if (_globalOverlay != null)
                {
                    _globalOverlay.Opacity = 0;
                }
            }
        }

        /// <summary>
        /// Applique le thème en chargeant le ResourceDictionary approprié
        /// </summary>
        private void ApplyTheme (AppTheme theme)
        {
            if (Application.Current == null) return;

            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            if (mergedDictionaries == null) return;

            // Supprimer l'ancien thème
            var existingTheme = mergedDictionaries
                .FirstOrDefault(d => d is LightTheme || d is DarkTheme);

            if (existingTheme != null)
            {
                mergedDictionaries.Remove(existingTheme);
            }

            // Charger le nouveau thème
            ResourceDictionary newTheme = theme == AppTheme.Dark
                ? new DarkTheme()
                : new LightTheme();

            mergedDictionaries.Add(newTheme);

            // Mettre à jour Application.UserAppTheme pour la cohérence
            Application.Current.UserAppTheme = theme;

            // Déclencher l'événement
            ThemeChanged?.Invoke(this, theme);

            _logger.LogDebug("🎨 ResourceDictionary chargé: {ThemeType}", newTheme.GetType().Name);
        }
    }
}
