using Microsoft.UI.Windowing;
using WinRT.Interop;
using WinUIColor = Windows.UI.Color;

namespace mobile.Platforms.Windows
{
    /// <summary>
    /// Helper pour thématiser la titlebar Windows (boutons système: Minimize, Maximize, Close)
    /// </summary>
    public static class WindowsTitleBarHelper
    {
        /// <summary>
        /// Applique le thème aux boutons système de la titlebar (_, □, X)
        /// </summary>
        public static void ApplyTheme(Microsoft.UI.Xaml.Window window)
        {
            try
            {
                var windowHandle = WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);

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
    }
}
