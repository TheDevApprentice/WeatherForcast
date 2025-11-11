//namespace mobile.Services.Notifications.Push
//{
//    /// <summary>
//    /// Configuration pour les notifications push
//    /// À remplir avec vos clés Firebase et APNS
//    /// </summary>
//    public class PushNotificationConfiguration
//    {
//        // ============================================
//        // FIREBASE (ANDROID)
//        // ============================================
//        /// <summary>
//        /// Clé serveur Firebase (Server Key)
//        /// Obtenue depuis: Firebase Console > Project Settings > Cloud Messaging > Server Key
//        /// </summary>
//        public string? FirebaseServerKey { get; set; }

//        /// <summary>
//        /// Sender ID Firebase
//        /// Obtenu depuis: Firebase Console > Project Settings > Cloud Messaging > Sender ID
//        /// </summary>
//        public string? FirebaseSenderId { get; set; }

//        /// <summary>
//        /// URL de l'API Firebase Cloud Messaging
//        /// </summary>
//        public string FirebaseApiUrl { get; set; } = "https://fcm.googleapis.com/fcm/send";

//        // ============================================
//        // APPLE PUSH NOTIFICATION SERVICE (iOS)
//        // ============================================
//        /// <summary>
//        /// Key ID de la clé APNs (.p8)
//        /// Obtenu depuis: Apple Developer > Certificates, Identifiers & Profiles > Keys
//        /// </summary>
//        public string? ApnsKeyId { get; set; }

//        /// <summary>
//        /// Team ID Apple
//        /// Obtenu depuis: Apple Developer > Membership
//        /// </summary>
//        public string? ApnsTeamId { get; set; }

//        /// <summary>
//        /// Bundle ID de l'application
//        /// Exemple: com.votreentreprise.weatherforecast
//        /// </summary>
//        public string? ApnsBundleId { get; set; }

//        /// <summary>
//        /// Chemin vers la clé privée APNs (.p8)
//        /// </summary>
//        public string? ApnsKeyPath { get; set; }

//        /// <summary>
//        /// URL de l'API APNs
//        /// - Development: https://api.sandbox.push.apple.com
//        /// - Production: https://api.push.apple.com
//        /// </summary>
//        public string ApnsApiUrl { get; set; } = "https://api.sandbox.push.apple.com";

//        // ============================================
//        // CONFIGURATION GÉNÉRALE
//        // ============================================
//        /// <summary>
//        /// Activer les notifications push
//        /// </summary>
//        public bool EnablePushNotifications { get; set; } = false;

//        /// <summary>
//        /// Activer les logs détaillés
//        /// </summary>
//        public bool EnableVerboseLogging { get; set; } = true;

//        /// <summary>
//        /// Durée de validité du token (en jours)
//        /// </summary>
//        public int TokenExpirationDays { get; set; } = 90;

//        /// <summary>
//        /// Nombre maximum de tentatives d'envoi
//        /// </summary>
//        public int MaxRetryAttempts { get; set; } = 3;

//        /// <summary>
//        /// Délai entre les tentatives (en secondes)
//        /// </summary>
//        public int RetryDelaySeconds { get; set; } = 5;

//        // ============================================
//        // VALIDATION
//        // ============================================
//        /// <summary>
//        /// Valide la configuration Firebase
//        /// </summary>
//        public bool IsFirebaseConfigured ()
//        {
//            return !string.IsNullOrEmpty(FirebaseServerKey) &&
//                   !string.IsNullOrEmpty(FirebaseSenderId);
//        }

//        /// <summary>
//        /// Valide la configuration APNS
//        /// </summary>
//        public bool IsApnsConfigured ()
//        {
//            return !string.IsNullOrEmpty(ApnsKeyId) &&
//                   !string.IsNullOrEmpty(ApnsTeamId) &&
//                   !string.IsNullOrEmpty(ApnsBundleId) &&
//                   !string.IsNullOrEmpty(ApnsKeyPath);
//        }

//        /// <summary>
//        /// Obtient une configuration par défaut (pour tests)
//        /// </summary>
//        public static PushNotificationConfiguration GetDefault ()
//        {
//            return new PushNotificationConfiguration
//            {
//                EnablePushNotifications = false,
//                EnableVerboseLogging = true,
//                FirebaseApiUrl = "https://fcm.googleapis.com/fcm/send",
//                ApnsApiUrl = "https://api.sandbox.push.apple.com",
//                TokenExpirationDays = 90,
//                MaxRetryAttempts = 3,
//                RetryDelaySeconds = 5
//            };
//        }
//    }
//}
