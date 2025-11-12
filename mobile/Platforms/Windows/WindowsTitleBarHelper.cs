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
        /// Utilise les couleurs définies dans LightTheme.xaml et DarkTheme.xaml
        /// </summary>
        public static void ApplyTheme(Microsoft.UI.Xaml.Window window)
        {
            try
            {
                var windowHandle = WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow?.TitleBar != null && Application.Current?.Resources != null)
                {
                    // Lire la couleur TitleBarForeground depuis les ressources actuelles
                    var foregroundMauiColor = GetColorResource("TitleBarForeground");
                    
                    if (foregroundMauiColor != null)
                    {
                        // Convertir Microsoft.Maui.Graphics.Color vers Windows.UI.Color
                        WinUIColor foregroundColor = WinUIColor.FromArgb(
                            (byte)(foregroundMauiColor.Alpha * 255),
                            (byte)(foregroundMauiColor.Red * 255),
                            (byte)(foregroundMauiColor.Green * 255),
                            (byte)(foregroundMauiColor.Blue * 255)
                        );

                        // Créer les couleurs de hover et pressed basées sur la foreground
                        bool isDark = foregroundMauiColor.Red > 0.5; // Si la couleur est claire, on est en dark mode
                        
                        WinUIColor hoverBackgroundColor = isDark
                            ? WinUIColor.FromArgb(32, 255, 255, 255)   // #FFFFFF20 pour dark mode
                            : WinUIColor.FromArgb(32, 0, 0, 0);        // #00000020 pour light mode

                        WinUIColor pressedBackgroundColor = isDark
                            ? WinUIColor.FromArgb(64, 255, 255, 255)   // #FFFFFF40 pour dark mode
                            : WinUIColor.FromArgb(64, 0, 0, 0);        // #00000040 pour light mode

                        WinUIColor inactiveForegroundColor = WinUIColor.FromArgb(
                            128,
                            (byte)(foregroundMauiColor.Red * 255),
                            (byte)(foregroundMauiColor.Green * 255),
                            (byte)(foregroundMauiColor.Blue * 255)
                        );

                        // Appliquer les couleurs
                        appWindow.TitleBar.ButtonForegroundColor = foregroundColor;
                        appWindow.TitleBar.ButtonBackgroundColor = WinUIColor.FromArgb(0, 0, 0, 0);

                        appWindow.TitleBar.ButtonHoverForegroundColor = foregroundColor;
                        appWindow.TitleBar.ButtonHoverBackgroundColor = hoverBackgroundColor;

                        appWindow.TitleBar.ButtonPressedForegroundColor = foregroundColor;
                        appWindow.TitleBar.ButtonPressedBackgroundColor = pressedBackgroundColor;

                        appWindow.TitleBar.ButtonInactiveForegroundColor = inactiveForegroundColor;
                        appWindow.TitleBar.ButtonInactiveBackgroundColor = WinUIColor.FromArgb(0, 0, 0, 0);
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Shell.Current.DisplayAlert("Debug WindowsTitleBarHelper", $"❌ Erreur lors de l'application du thème titlebar: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        /// <summary>
        /// Récupère une couleur depuis les ressources de l'application
        /// </summary>
        private static Microsoft.Maui.Graphics.Color? GetColorResource(string key)
        {
            try
            {
                if (Application.Current?.Resources.TryGetValue(key, out var value) == true)
                {
                    return value as Microsoft.Maui.Graphics.Color;
                }
            }
            catch { }
            return null;
        }
    }
}
