using mobile.Services.Internal.Interfaces;

namespace mobile
{
    public partial class AppShellMobile : Shell
    {
        private readonly IOfflineBannerManager _bannerManager;

        public AppShellMobile (IOfflineBannerManager bannerManager)
        {
            _bannerManager = bannerManager;

            InitializeComponent();

            // Les routes sont gérées par navigation directe avec Shell.Current.Navigation.PushAsync
            // au lieu de Routing.RegisterRoute car les pages utilisent l'injection de dépendances

            // Ré-appliquer le bandeau à chaque navigation
            this.Navigated += (_, __) => _bannerManager.ApplyToCurrentPage();
        }

        /// <summary>
        /// Initialise le NetworkMonitor (appelé depuis App.xaml.cs après que le Shell soit prêt)
        /// </summary>
        public void InitializeNetworkMonitor (INetworkMonitorService networkMonitor)
        {
            _bannerManager.Initialize(networkMonitor);
            _bannerManager.ApplyToCurrentPage();
        }
    }
}
