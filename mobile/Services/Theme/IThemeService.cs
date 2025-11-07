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
        Task SetThemeAsync(AppTheme theme, bool animated = true);

        /// <summary>
        /// Initialise le service avec l'overlay global pour les animations
        /// </summary>
        /// <param name="overlay">BoxView utilisé pour l'animation de transition</param>
        void RegisterGlobalOverlay(BoxView overlay);
    }
}
