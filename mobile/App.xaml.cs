using mobile.Services;

namespace mobile
{
    public partial class App : Application
    {
        private readonly ISecureStorageService _secureStorage;

        public App(ISecureStorageService secureStorage)
        {
            InitializeComponent();
            _secureStorage = secureStorage;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell();
            
            // Vérifier l'authentification au démarrage
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var isAuthenticated = await _secureStorage.IsAuthenticatedAsync();
                
                // Mettre à jour l'UI du Shell selon l'état d'authentification
                shell.UpdateAuthenticationUI(isAuthenticated);
                
                // Naviguer vers la bonne page
                if (isAuthenticated)
                {
                    await shell.GoToAsync("///main");
                }
                else
                {
                    await shell.GoToAsync("///login");
                }
            });

            return new Window(shell);
        }
    }
}