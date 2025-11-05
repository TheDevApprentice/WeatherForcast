using Microsoft.Extensions.Logging;

namespace mobile.Services
{
    /// <summary>
    /// Service de surveillance de la connectivité réseau en temps réel
    /// Permet d'éviter les appels HTTP inutiles quand le réseau n'est pas disponible
    /// </summary>
    public class NetworkMonitorService : INetworkMonitorService
    {
        private readonly ILogger<NetworkMonitorService> _logger;
        private NetworkAccess _currentAccess;
        private bool _isMonitoring;

        /// <summary>
        /// Événement déclenché quand la connectivité change
        /// </summary>
        public event EventHandler<NetworkAccess>? ConnectivityChanged;

        public NetworkMonitorService(ILogger<NetworkMonitorService> logger)
        {
            _logger = logger;
            _currentAccess = Connectivity.NetworkAccess;
        }

        /// <summary>
        /// Démarre la surveillance du réseau
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _currentAccess = Connectivity.NetworkAccess;

            // S'abonner aux changements de connectivité
            Connectivity.ConnectivityChanged += OnConnectivityChanged;

#if DEBUG
            _logger.LogInformation("📡 NetworkMonitor démarré - État initial: {Status}", 
                IsNetworkAvailable ? "En ligne" : "Hors ligne");
#endif
        }

        /// <summary>
        /// Arrête la surveillance du réseau
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            _isMonitoring = false;
            Connectivity.ConnectivityChanged -= OnConnectivityChanged;

#if DEBUG
            _logger.LogInformation("📡 NetworkMonitor arrêté");
#endif
        }

        /// <summary>
        /// Vérifie si le réseau est disponible (Internet accessible)
        /// </summary>
        public bool IsNetworkAvailable => _currentAccess == NetworkAccess.Internet;

        /// <summary>
        /// Obtient l'état actuel du réseau
        /// </summary>
        public NetworkAccess CurrentAccess => _currentAccess;

        /// <summary>
        /// Gestionnaire d'événement pour les changements de connectivité
        /// </summary>
        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            var previousAccess = _currentAccess;
            _currentAccess = e.NetworkAccess;

            // Ne notifier que si l'état a vraiment changé (Internet disponible ou non)
            var wasAvailable = previousAccess == NetworkAccess.Internet;
            var isAvailable = _currentAccess == NetworkAccess.Internet;

            if (wasAvailable != isAvailable)
            {
#if DEBUG
                _logger.LogInformation("📡 Connectivité changée: {Previous} → {Current}", 
                    wasAvailable ? "En ligne" : "Hors ligne",
                    isAvailable ? "En ligne" : "Hors ligne");
#endif

                // Notifier les abonnés
                ConnectivityChanged?.Invoke(this, _currentAccess);
            }
        }

        /// <summary>
        /// Force une vérification immédiate de la connectivité
        /// </summary>
        public void RefreshNetworkStatus()
        {
            var newAccess = Connectivity.NetworkAccess;
            if (newAccess != _currentAccess)
            {
                var previousAccess = _currentAccess;
                _currentAccess = newAccess;

                var wasAvailable = previousAccess == NetworkAccess.Internet;
                var isAvailable = _currentAccess == NetworkAccess.Internet;

                if (wasAvailable != isAvailable)
                {
#if DEBUG
                    _logger.LogInformation("📡 Refresh connectivité: {Status}", 
                        isAvailable ? "En ligne" : "Hors ligne");
#endif

                    ConnectivityChanged?.Invoke(this, _currentAccess);
                }
            }
        }
    }
}
