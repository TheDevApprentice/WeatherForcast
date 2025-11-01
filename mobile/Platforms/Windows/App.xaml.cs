using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WinUIColor = Windows.UI.Color;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace mobile.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            // Appliquer le thème aux boutons système de la titlebar (Minimize, Maximize, Close)
            var window = Microsoft.UI.Xaml.Window.Current;
            if (window != null)
            {
                ApplyTitleBarTheme(window);
            }
        }

        /// <summary>
        /// Applique le thème aux boutons système de la titlebar (_, □, X)
        /// </summary>
        private void ApplyTitleBarTheme(Microsoft.UI.Xaml.Window window)
        {
            try
            {
                var appWindow = GetAppWindow(window);
                if (appWindow?.TitleBar != null)
                {
                    // Couleurs des boutons système (Minimize, Maximize, Close)
                    // État normal
                    appWindow.TitleBar.ButtonForegroundColor = WinUIColor.FromArgb(255, 255, 255, 255); // White
                    appWindow.TitleBar.ButtonBackgroundColor = WinUIColor.FromArgb(0, 0, 0, 0); // Transparent

                    // État hover (survol)
                    appWindow.TitleBar.ButtonHoverForegroundColor = WinUIColor.FromArgb(255, 255, 255, 255); // White
                    appWindow.TitleBar.ButtonHoverBackgroundColor = WinUIColor.FromArgb(32, 255, 255, 255); // #FFFFFF20

                    // État pressed (clic)
                    appWindow.TitleBar.ButtonPressedForegroundColor = WinUIColor.FromArgb(255, 255, 255, 255); // White
                    appWindow.TitleBar.ButtonPressedBackgroundColor = WinUIColor.FromArgb(64, 255, 255, 255); // #FFFFFF40

                    // Boutons inactifs
                    appWindow.TitleBar.ButtonInactiveForegroundColor = WinUIColor.FromArgb(128, 255, 255, 255); // #FFFFFF80
                    appWindow.TitleBar.ButtonInactiveBackgroundColor = WinUIColor.FromArgb(0, 0, 0, 0); // Transparent
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de l'application du thème titlebar: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère l'AppWindow depuis une Window MAUI
        /// </summary>
        private AppWindow? GetAppWindow(Microsoft.UI.Xaml.Window window)
        {
            try
            {
                var windowHandle = WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                return AppWindow.GetFromWindowId(windowId);
            }
            catch
            {
                return null;
            }
        }

    }
}
