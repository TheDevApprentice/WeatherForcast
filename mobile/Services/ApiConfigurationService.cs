using Microsoft.Extensions.Configuration;

namespace mobile.Services
{
    /// <summary>
    /// Implémentation du service de configuration API
    /// Centralise la logique de résolution d'URL selon la plateforme
    /// </summary>
    public class ApiConfigurationService : IApiConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;

        public ApiConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            _baseUrl = ResolveBaseUrl();
        }

        /// <summary>
        /// Obtient l'URL de base de l'API
        /// </summary>
        public string GetBaseUrl() => _baseUrl;

        /// <summary>
        /// Construit l'URL complète d'un hub SignalR
        /// </summary>
        public string GetHubUrl(string hubPath)
        {
            if (string.IsNullOrWhiteSpace(hubPath))
            {
                throw new ArgumentException("Le chemin du hub ne peut pas être vide", nameof(hubPath));
            }

            // Retirer le slash final de l'URL de base si présent
            var baseUrl = _baseUrl.TrimEnd('/');

            // S'assurer que le chemin du hub commence par un slash
            if (!hubPath.StartsWith('/'))
            {
                hubPath = $"/{hubPath}";
            }

            return $"{baseUrl}{hubPath}";
        }

        /// <summary>
        /// Résout l'URL de base selon la plateforme et la configuration
        /// </summary>
        private string ResolveBaseUrl()
        {
            var baseUrl = "";

#if ANDROID
            // Android: Utilise BaseUrlDevice pour téléphone réel, BaseUrlEmulator pour émulateur
            baseUrl = _configuration["ApiSettings:BaseUrlDevice"]
                ?? _configuration["ApiSettings:BaseUrlEmulator"];
#elif IOS
            // iOS: Utilise BaseUrlDevice pour iPhone réel, BaseUrlEmulator pour simulateur
            baseUrl = _configuration["ApiSettings:BaseUrlDevice"] 
                ?? _configuration["ApiSettings:BaseUrlEmulator"];
#elif MACCATALYST
            // MacCatalyst: Utilise BaseUrlDevice
            baseUrl = _configuration["ApiSettings:BaseUrlDevice"];
#elif WINDOWS
            // Windows: Utilise BaseUrlWindows
            baseUrl = _configuration["ApiSettings:BaseUrlWindows"];
#else
            // Fallback pour autres plateformes
            baseUrl = _configuration["ApiSettings:BaseUrlDevice"];
#endif

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "ApiSettings:BaseUrl* n'est pas configuré dans appsettings.json. " +
                    "Veuillez configurer BaseUrlDevice, BaseUrlEmulator ou BaseUrlWindows selon votre plateforme.");
            }

            return baseUrl;
        }
    }
}
