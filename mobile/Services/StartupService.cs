using Microsoft.Extensions.Logging;
using mobile.Exceptions;
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

        public StartupService (
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
        public async Task<bool> ExecuteStartupProceduresAsync (IProgress<StartupProcedure> progress)
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
        private async Task<StartupProcedureResult> CheckNetworkConnectivityAsync ()
        {
            // Simulation de temps de chargement pour voir l'étape
            await Task.Delay(500);

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
        private async Task<StartupProcedureResult> CheckApiAvailabilityAsync ()
        {
            // Simulation de temps de chargement pour voir l'étape
            await Task.Delay(500);

            const int maxRetries = 4;
            const int delayMs = 1000;

            try
            {
                // Essayer de valider via l'API (mode online)

                await _apiAuthService.CheckApiAvailabilityAsync();

                // Si on arrive ici sans exception, l'API est joignable
                _logger.LogInformation("API joignable");
                return StartupProcedureResult.Ok();
            }
            catch (ApiUnavailableException ex)
            {
                return StartupProcedureResult.Fail(
                        "L'API n'est pas joignable. Veuillez vérifier que le serveur est démarré.",
                        canContinue: true);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la vérification de l'API");

                return StartupProcedureResult.Fail(
                    $"Erreur lors de la connexion à l'API: {ex.Message}",
                    canContinue: false);
            }
        }

        /// <summary>
        /// Procédure 3: Valider la session utilisateur (avec support offline)
        /// </summary>
        private async Task<StartupProcedureResult> ValidateUserSessionAsync ()
        {
            // Simulation de temps de chargement pour voir l'étape
            await Task.Delay(500);

            try
            {
                var hasToken = await VerifyHasToken();
                if (!hasToken)
                {
                    _logger.LogInformation("Aucun token, pas de session à valider");
                    return StartupProcedureResult.Ok(); // Pas d'erreur, juste pas de session
                }

                var isTokenValid = await _secureStorage.IsTokenValidAsync();
                if (!isTokenValid)
                {
                    _logger.LogWarning("❌ Token expiré, nettoyage de la session");
                    await _secureStorage.ClearAllAsync();
                    await _authState.ClearStateAsync();
                    return StartupProcedureResult.Ok(); // Token expiré, redirection vers login
                }

                _logger.LogInformation("✅ Token valide localement");

                //// Essayer de valider via l'API (mode online)

                try
                {
                    // Vérifier d'abord si l'API est joignable
                    await _apiAuthService.CheckApiAvailabilityAsync();

                    // API joignable, valider la session
                    var isValid = await _sessionValidation.ValidateSessionAsync();

                    if (!isValid)
                    {
                        _logger.LogWarning("❌ Session invalide selon l'API, nettoyage...");
                        await _sessionValidation.ClearSessionAsync();
                        await _authState.ClearStateAsync();
                        return StartupProcedureResult.Ok(); // Session invalide, redirection vers login
                    }

                    // Session valide : récupérer les infos utilisateur depuis l'API
                    _logger.LogInformation("✅ Session valide (mode online)");
                    var currentUser = await _apiAuthService.GetCurrentUserAsync();

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
                catch (ApiUnavailableException ex)
                {
                    // API non joignable, mais token valide localement -> Mode offline
                    _logger.LogWarning(ex, "📡 API non joignable, activation du mode offline");

                    // Extraire les infos du token JWT pour authentification offline
                    var userInfo = await _secureStorage.GetUserInfoFromTokenAsync();

                    if (userInfo.HasValue)
                    {
                        var (userId, email, firstName, lastName) = userInfo.Value;

                        // Sauvegarder l'état d'authentification en mode offline
                        var authState = AuthenticationState.Authenticated(
                            userId,
                            email,
                            firstName,
                            lastName
                        );

                        await _authState.SetStateAsync(authState);
                        _logger.LogInformation("✅ Authentification offline réussie pour {Email}", email);

                        return StartupProcedureResult.Ok();
                    }
                    else
                    {
                        _logger.LogWarning("❌ Impossible d'extraire les infos du token");
                        await _secureStorage.ClearAllAsync();
                        await _authState.ClearStateAsync();
                        return StartupProcedureResult.Fail("Impossible d'extraire les infos du token", canContinue: true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation de session");
                // On continue même si la validation échoue
                return StartupProcedureResult.Fail("Erreur lors de la validation de session", canContinue: false);
            }
        }

        #endregion

        private async Task<bool> VerifyHasToken ()
        {
            // Vérifier si un token existe
            var hasToken = await _secureStorage.IsAuthenticatedAsync();

            if (!hasToken)
            {
                _logger.LogInformation("Aucun token, pas de session à valider");
                return !hasToken; // Pas d'erreur, juste pas de session
            }

            return hasToken;
        }
    }

}
