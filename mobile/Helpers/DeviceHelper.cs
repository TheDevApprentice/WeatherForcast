namespace mobile.Helpers
{
    /// <summary>
    /// Helper pour détecter le type d'appareil, la plateforme et l'orientation
    /// </summary>
    public static class DeviceHelper
    {
        /// <summary>
        /// Type d'appareil détecté
        /// </summary>
        public enum DeviceType
        {
            Phone,
            Tablet,
            Desktop
        }

        /// <summary>
        /// Orientation de l'appareil
        /// </summary>
        public enum DeviceOrientation
        {
            Portrait,
            Landscape
        }

        /// <summary>
        /// Plateforme de l'appareil
        /// </summary>
        public enum DevicePlatform
        {
            Android,
            iOS,
            Windows,
            MacCatalyst,
            Unknown
        }

        /// <summary>
        /// Obtient le type d'appareil actuel (Phone, Tablet, Desktop)
        /// </summary>
        public static DeviceType GetDeviceType()
        {
#if ANDROID || IOS
            var displayInfo = DeviceDisplay.MainDisplayInfo;
            var width = displayInfo.Width / displayInfo.Density;
            var height = displayInfo.Height / displayInfo.Density;
            
            // Tablette si la plus grande dimension est >= 600dp (standard Android/iOS)
            var maxDimension = Math.Max(width, height);
            
            return maxDimension >= 600 ? DeviceType.Tablet : DeviceType.Phone;
#else
            // Windows et MacCatalyst sont considérés comme Desktop
            return DeviceType.Desktop;
#endif
        }

        /// <summary>
        /// Obtient l'orientation actuelle de l'appareil
        /// </summary>
        public static DeviceOrientation GetOrientation()
        {
            var displayInfo = DeviceDisplay.MainDisplayInfo;
            var width = displayInfo.Width / displayInfo.Density;
            var height = displayInfo.Height / displayInfo.Density;
            
            return width > height ? DeviceOrientation.Landscape : DeviceOrientation.Portrait;
        }

        /// <summary>
        /// Obtient la plateforme actuelle
        /// </summary>
        public static DevicePlatform GetPlatform()
        {
#if ANDROID
            return DevicePlatform.Android;
#elif IOS
            return DevicePlatform.iOS;
#elif WINDOWS
            return DevicePlatform.Windows;
#elif MACCATALYST
            return DevicePlatform.MacCatalyst;
#else
            return DevicePlatform.Unknown;
#endif
        }

        /// <summary>
        /// Vérifie si l'appareil est un téléphone
        /// </summary>
        public static bool IsPhone() => GetDeviceType() == DeviceType.Phone;

        /// <summary>
        /// Vérifie si l'appareil est une tablette
        /// </summary>
        public static bool IsTablet() => GetDeviceType() == DeviceType.Tablet;

        /// <summary>
        /// Vérifie si l'appareil est un desktop (Windows ou MacCatalyst)
        /// </summary>
        public static bool IsDesktop() => GetDeviceType() == DeviceType.Desktop;

        /// <summary>
        /// Vérifie si l'appareil est en mode portrait
        /// </summary>
        public static bool IsPortrait() => GetOrientation() == DeviceOrientation.Portrait;

        /// <summary>
        /// Vérifie si l'appareil est en mode paysage
        /// </summary>
        public static bool IsLandscape() => GetOrientation() == DeviceOrientation.Landscape;

        /// <summary>
        /// Vérifie si l'appareil est une tablette en mode paysage
        /// </summary>
        public static bool IsTabletLandscape() => IsTablet() && IsLandscape();

        /// <summary>
        /// Vérifie si l'appareil est une tablette en mode portrait
        /// </summary>
        public static bool IsTabletPortrait() => IsTablet() && IsPortrait();

        /// <summary>
        /// Vérifie si l'appareil est un téléphone en mode paysage
        /// </summary>
        public static bool IsPhoneLandscape() => IsPhone() && IsLandscape();

        /// <summary>
        /// Vérifie si l'appareil est un téléphone en mode portrait
        /// </summary>
        public static bool IsPhonePortrait() => IsPhone() && IsPortrait();

        /// <summary>
        /// Détermine si l'appareil doit utiliser un layout de type desktop
        /// (Desktop ou Tablette en mode paysage)
        /// </summary>
        public static bool ShouldUseDesktopLayout() => IsDesktop() || IsTabletLandscape();

        /// <summary>
        /// Détermine si l'appareil doit utiliser un layout de type mobile
        /// (Téléphone ou Tablette en mode portrait)
        /// </summary>
        public static bool ShouldUseMobileLayout() => !ShouldUseDesktopLayout();

        /// <summary>
        /// Obtient la largeur de l'écran en unités indépendantes de la densité (dp)
        /// </summary>
        public static double GetScreenWidth()
        {
            var displayInfo = DeviceDisplay.MainDisplayInfo;
            return displayInfo.Width / displayInfo.Density;
        }

        /// <summary>
        /// Obtient la hauteur de l'écran en unités indépendantes de la densité (dp)
        /// </summary>
        public static double GetScreenHeight()
        {
            var displayInfo = DeviceDisplay.MainDisplayInfo;
            return displayInfo.Height / displayInfo.Density;
        }

        /// <summary>
        /// Obtient les informations complètes sur l'appareil sous forme de chaîne
        /// </summary>
        public static string GetDeviceInfo()
        {
            var platform = GetPlatform();
            var deviceType = GetDeviceType();
            var orientation = GetOrientation();
            var width = GetScreenWidth();
            var height = GetScreenHeight();

            return $"Platform: {platform}, Type: {deviceType}, Orientation: {orientation}, Size: {width:F0}x{height:F0}dp";
        }
    }
}
