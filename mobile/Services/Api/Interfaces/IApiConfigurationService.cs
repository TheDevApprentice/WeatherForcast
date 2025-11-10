namespace mobile.Services.Api.Interfaces
{
    /// <summary>
    /// Service de configuration centralisée pour les URLs API et SignalR
    /// Élimine la duplication de code de configuration d'URL entre services
    /// </summary>
    public interface IApiConfigurationService
    {
        /// <summary>
        /// Obtient l'URL de base de l'API selon la plateforme
        /// </summary>
        string GetBaseUrl ();

        /// <summary>
        /// Construit l'URL complète d'un hub SignalR
        /// </summary>
        /// <param name="hubPath">Chemin du hub (ex: "/hubs/weatherforecast")</param>
        string GetHubUrl (string hubPath);
    }
}
