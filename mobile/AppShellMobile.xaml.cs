using mobile.Services.Internal.Interfaces;

namespace mobile
{
    public partial class AppShellMobile : Shell
    {
        private readonly IOfflineBannerManager _bannerManager;
        private readonly INetworkMonitorService _networkMonitor;

        public AppShellMobile (IOfflineBannerManager bannerManager, INetworkMonitorService networkMonitor)
        {
            _networkMonitor = networkMonitor;
            _bannerManager = bannerManager;

            InitializeComponent();

            // Les routes sont gérées par navigation directe avec Shell.Current.Navigation.PushAsync
            // au lieu de Routing.RegisterRoute car les pages utilisent l'injection de dépendances

            // Ré-appliquer le bandeau à chaque navigation
            this.Navigated += (_, __) => _bannerManager.ApplyToCurrentPage();

            _bannerManager.Initialize(networkMonitor);
            _bannerManager.ApplyToCurrentPage();
        }
    }
}
