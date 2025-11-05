namespace mobile.Services
{
    /// <summary>
    /// Interface pour le service de surveillance de la connectivité réseau
    /// </summary>
    public interface INetworkMonitorService
    {
        /// <summary>
        /// Événement déclenché quand la connectivité change
        /// </summary>
        event EventHandler<NetworkAccess>? ConnectivityChanged;

        /// <summary>
        /// Vérifie si le réseau est disponible (Internet accessible)
        /// </summary>
        bool IsNetworkAvailable { get; }

        /// <summary>
        /// Obtient l'état actuel du réseau
        /// </summary>
        NetworkAccess CurrentAccess { get; }

        /// <summary>
        /// Démarre la surveillance du réseau
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Arrête la surveillance du réseau
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Force une vérification immédiate de la connectivité
        /// </summary>
        void RefreshNetworkStatus();
    }
}
