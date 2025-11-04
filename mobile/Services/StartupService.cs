using Microsoft.Extensions.Logging;
using mobile.Models;
using System.Net.NetworkInformation;

namespace mobile.Services
{
    /// <summary>
    /// Service de gestion des procédures de démarrage de l'application
    /// </summary>
    public class StartupService : IStartupService
    {
        private readonly ILogger<StartupService> _logger;
        private readonly IApiAuthService _apiAuthService;
        private readonly ISessionValidationService _sessionValidation;
        private readonly ISecureStorageService _secureStorage;
        private readonly IAuthenticationStateService _authState;
        private readonly List<StartupProcedure> _procedures;

        public IReadOnlyList<StartupProcedure> Procedures => _procedures.AsReadOnly();

        public StartupService(
            ILogger<StartupService> logger,
            IServiceProvider serviceProvider,
            ISecureStorageService secureStorage,
            IAuthenticationStateService authState)
        {
            _logger = logger;
            _secureStorage = secureStorage;
            _authState = authState;

            // Résoudre les services via ServiceProvider pour éviter les problèmes de lifetime
            using var scope = serviceProvider.CreateScope();
            _apiAuthService = scope.ServiceProvider.GetRequiredService<IApiAuthService>();
            _sessionValidation = scope.ServiceProvider.GetRequiredService<ISessionValidationService>();

            // Initialiser la queue de procédures
            _procedures = new List<StartupProcedure>
            {
                new StartupProcedure
                {
                    Name = "Vérification du réseau",
                    Description = "Vérification de la connectivité réseau...",
                    ExecuteAsync = CheckNetworkConnectivityAsync
                },
                new StartupProcedure
                {
                    Name = "Connexion à l'API",
                    Description = "Vérification de la disponibilité de l'API...",
                    ExecuteAsync = CheckApiAvailabilityAsync
                },
                new StartupProcedure
                {
                    Name = "Validation de session",
                    Description = "Vérification de la session utilisateur...",
                    ExecuteAsync = ValidateUserSessionAsync
                }
            };
        }

        /// <summary>
        /// Exécute toutes les procédures de démarrage dans l'ordre
        /// </summary>
        public async Task<bool> ExecuteStartupProceduresAsync(IProgress<StartupProcedure> progress)
        {
            _logger.LogInformation("🚀 Début des procédures de démarrage");

            foreach (var procedure in _procedures)
            {
                try
                {
                    // Mettre à jour le statut : Running
                    procedure.Status = StartupProcedureStatus.Running;
                    progress?.Report(procedure);

                    _logger.LogInformation("▶️ Exécution: {Name}", procedure.Name);

                    // Exécuter la procédure
                    var result = await procedure.ExecuteAsync();

                    if (result.Success)
                    {
                        procedure.Status = StartupProcedureStatus.Success;
                        _logger.LogInformation("✅ {Name} - Succès", procedure.Name);
                    }
                    else
                    {
                        procedure.Status = StartupProcedureStatus.Failed;
                        procedure.ErrorMessage = result.ErrorMessage;
                        _logger.LogWarning("❌ {Name} - Échec: {Error}", procedure.Name, result.ErrorMessage);

                        progress?.Report(procedure);

                        // Si on ne peut pas continuer, arrêter la queue
                        if (!result.CanContinue)
                        {
                            _logger.LogError("🛑 Arrêt des procédures de démarrage");
                            return false;
                        }
                    }

                    progress?.Report(procedure);
                }
                catch (Exception ex)
                {
                    procedure.Status = StartupProcedureStatus.Failed;
                    procedure.ErrorMessage = $"Erreur inattendue: {ex.Message}";
                    _logger.LogError(ex, "❌ Erreur lors de l'exécution de {Name}", procedure.Name);

                    progress?.Report(procedure);
                    return false;
                }
            }

            _logger.LogInformation("✅ Toutes les procédures de démarrage terminées");
            return true;
        }

        #region Procédures de démarrage

        /// <summary>
        /// Procédure 1: Vérifier la connectivité réseau
        /// </summary>
        private async Task<StartupProcedureResult> CheckNetworkConnectivityAsync()
        {
            // Simulation de temps de chargement pour voir l'étape
            await Task.Delay(2000);

            try
            {
                // Vérifier si le réseau est accessible
                var isNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();

                if (!isNetworkAvailable)
                {
                    return StartupProcedureResult.Fail(
                        "Aucune connexion réseau détectée. Veuillez vérifier votre connexion.",
                        canContinue: false);
                }

                _logger.LogInformation("Réseau disponible");
                return StartupProcedureResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du réseau");
                return StartupProcedureResult.Fail(
                    "Impossible de vérifier la connectivité réseau.",
                    canContinue: false);
            }
        }

        /// <summary>
        /// Procédure 2: Vérifier la disponibilité de l'API (avec retry)
        /// </summary>
        private async Task<StartupProcedureResult> CheckApiAvailabilityAsync()
        {
            // Simulation de temps de chargement pour voir l'étape
            await Task.Delay(2000);

            const int maxRetries = 4;
            const int delayMs = 1000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Tentative {Attempt}/{Max} de connexion à l'API...", attempt, maxRetries);

                    // Utiliser le ServiceProvider pour créer un scope et résoudre IApiAuthService
                    using var scope = ((IServiceProvider)Application.Current!.Handler!.MauiContext!.Services).CreateScope();
                    var apiAuthService = scope.ServiceProvider.GetRequiredService<IApiAuthService>();

                    // Tenter un appel simple à l'API (par exemple, /me sans authentification)
                    var user = await apiAuthService.GetCurrentUserAsync();

                    // Si on arrive ici sans exception, l'API est joignable
                    _logger.LogInformation("API joignable");
                    return StartupProcedureResult.Ok();
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning("Tentative {Attempt}/{Max} échouée: {Message}", attempt, maxRetries, ex.Message);

                    if (attempt == maxRetries)
                    {
                        return StartupProcedureResult.Fail(
                            "L'API n'est pas joignable. Veuillez vérifier que le serveur est démarré.",
                            canContinue: false);
                    }

                    // Attendre avant de réessayer
                    await Task.Delay(delayMs * attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur inattendue lors de la vérification de l'API");

                    if (attempt == maxRetries)
                    {
                        return StartupProcedureResult.Fail(
                            $"Erreur lors de la connexion à l'API: {ex.Message}",
                            canContinue: false);
                    }

                    await Task.Delay(delayMs * attempt);
                }
            }

            return StartupProcedureResult.Fail(
                "Impossible de se connecter à l'API après plusieurs tentatives.",
                canContinue: false);
        }

        /// <summary>
        /// Procédure 3: Valider la session utilisateur
        /// </summary>
        private async Task<StartupProcedureResult> ValidateUserSessionAsync()
        {
            // Simulation de temps de chargement pour voir l'étape
            await Task.Delay(2000);

            try
            {
                // Vérifier si un token existe
                var hasToken = await _secureStorage.IsAuthenticatedAsync();

                if (!hasToken)
                {
                    _logger.LogInformation("Aucun token, pas de session à valider");
                    return StartupProcedureResult.Ok(); // Pas d'erreur, juste pas de session
                }

                // Valider la session via l'API
                using var scope = ((IServiceProvider)Application.Current!.Handler!.MauiContext!.Services).CreateScope();
                var sessionValidation = scope.ServiceProvider.GetRequiredService<ISessionValidationService>();
                var apiAuthService = scope.ServiceProvider.GetRequiredService<IApiAuthService>();
                
                var isValid = await sessionValidation.ValidateSessionAsync();

                if (!isValid)
                {
                    _logger.LogWarning("Session invalide, nettoyage...");
                    await sessionValidation.ClearSessionAsync();
                    await _authState.ClearStateAsync();
                    return StartupProcedureResult.Ok(); // Pas d'erreur, juste session invalide
                }

                // Session valide : récupérer les infos utilisateur et sauvegarder l'état
                _logger.LogInformation("Session valide, récupération des informations utilisateur...");
                var currentUser = await apiAuthService.GetCurrentUserAsync();

                if (currentUser != null)
                {
                    // Sauvegarder l'état d'authentification
                    var authState = AuthenticationState.Authenticated(
                        currentUser.Id,
                        currentUser.Email,
                        currentUser.FirstName,
                        currentUser.LastName
                    );

                    await _authState.SetStateAsync(authState);
                    _logger.LogInformation("État d'authentification sauvegardé pour {Email}", currentUser.Email);
                }

                return StartupProcedureResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation de session");
                // On continue même si la validation échoue
                return StartupProcedureResult.Ok();
            }
        }

        #endregion
    }
}
