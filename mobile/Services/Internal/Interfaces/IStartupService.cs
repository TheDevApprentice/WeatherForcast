using mobile.Models;

namespace mobile.Services.Internal.Interfaces
{
    /// <summary>
    /// Service de gestion des procédures de démarrage de l'application
    /// </summary>
    public interface IStartupService
    {
        /// <summary>
        /// Exécute toutes les procédures de démarrage dans l'ordre
        /// </summary>
        Task<bool> ExecuteStartupProceduresAsync(IProgress<StartupProcedure> progress);

        /// <summary>
        /// Liste des procédures de démarrage
        /// </summary>
        IReadOnlyList<StartupProcedure> Procedures { get; }
    }
}
