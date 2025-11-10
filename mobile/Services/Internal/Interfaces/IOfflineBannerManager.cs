namespace mobile.Services.Internal.Interfaces
{
    /// <summary>
    /// Interface pour le gestionnaire du bandeau hors ligne
    /// </summary>
    public interface IOfflineBannerManager
    {
        /// <summary>
        /// Initialise le gestionnaire avec le service de monitoring réseau
        /// </summary>
        void Initialize(INetworkMonitorService networkMonitor);

        /// <summary>
        /// Applique le bandeau sur la page courante
        /// </summary>
        void ApplyToCurrentPage();

        /// <summary>
        /// Nettoie les ressources
        /// </summary>
        void Cleanup();
    }
}
