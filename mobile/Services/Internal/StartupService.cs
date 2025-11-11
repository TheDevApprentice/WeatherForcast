using Microsoft.Extensions.Logging;
using mobile.Exceptions;
using mobile.Services.Api.Interfaces;
using mobile.Services.Internal.Interfaces;

namespace mobile.Services.Internal
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
        private readonly INetworkMonitorService _networkMonitor;

        public IReadOnlyList<StartupProcedure> Procedures => _procedures.AsReadOnly();

        public StartupService (
            ILogger<StartupService> logger,
            IServiceProvider serviceProvider,
            ISecureStorageService secureStorage,
            IAuthenticationStateService authState,
            INetworkMonitorService networkMonitor)
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
            _networkMonitor = networkMonitor;
        }

        /// <summary>
        /// Exécute toutes les procédures de démarrage dans l'ordre
        /// </summary>
        public async Task<bool> ExecuteStartupProceduresAsync (IProgress<StartupProcedure> progress)
        {
#if DEBUG
            _logger.LogInformation("🚀 Début des procédures de démarrage");
#endif

            foreach (var procedure in _procedures)
            {
                try
                {
                    // Mettre à jour le statut : Running
                    procedure.Status = StartupProcedureStatus.Running;
                    progress?.Report(procedure);

#if DEBUG
                    _logger.LogInformation("▶️ Exécution: {Name}", procedure.Name);
#endif

                    // Exécuter la procédure
                    var result = await procedure.ExecuteAsync();

                    if (result.Success)
                    {
                        procedure.Status = StartupProcedureStatus.Success;
#if DEBUG
                        _logger.LogInformation("✅ {Name} - Succès", procedure.Name);
#endif
                    }
                    else
                    {
                        procedure.Status = StartupProcedureStatus.Failed;
                        procedure.ErrorMessage = result.ErrorMessage;
#if DEBUG
                        _logger.LogWarning("❌ {Name} - Échec: {Error}", procedure.Name, result.ErrorMessage);
#endif

                        progress?.Report(procedure);

                        // Si on ne peut pas continuer, arrêter la queue
                        if (!result.CanContinue)
                        {
#if DEBUG
                            _logger.LogError("🛑 Arrêt des procédures de démarrage");
#endif
                            return false;
                        }
                    }

                    progress?.Report(procedure);
                }
                catch (Exception ex)
                {
                    procedure.Status = StartupProcedureStatus.Failed;
                    procedure.ErrorMessage = $"Erreur inattendue: {ex.Message}";

#if DEBUG
                    _logger.LogError(ex, "❌ Erreur lors de l'exécution de {Name}", procedure.Name);
#endif

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
                if (!_networkMonitor.IsNetworkAvailable)
                {
#if DEBUG
                    _logger.LogInformation("Réseau indisponible");
#endif
                    throw new NetworkUnavailableExecption();
                }

#if DEBUG
                _logger.LogInformation("Réseau disponible");
#endif
                return StartupProcedureResult.Ok();
            }
            catch (NetworkUnavailableExecption ex)
            {
                return StartupProcedureResult.Fail(
                        ex.UserMessage,
                        canContinue: true);
            }
            catch (Exception ex)
            {
#if DEBUG
                _logger.LogError(ex, "Erreur lors de la vérification du réseau. Erreur grave");
#endif
                return StartupProcedureResult.Fail(
                    "Impossible de vérifier la connectivité réseau. Veuillez réessayer ultérieurement",
                    canContinue: false);
            }
        }

        /// <summary>
        /// Procédure 2: Vérifier la disponibilité de l'API (avec retry)
        /// </summary>
        private async Task<StartupProcedureResult> CheckApiAvailabilityAsync ()
        {
            // Simulation de temps de chargement pour voir l'étape
#if DEBUG
            await Task.Delay(500);
#endif

            try
            {
                // Essayer de valider via l'API (mode online)
                await _apiAuthService.CheckApiAvailabilityAsync();

                // Si on arrive ici sans exception, l'API est joignable
#if DEBUG
                _logger.LogInformation("API joignable");
#endif
                return StartupProcedureResult.Ok();
            }
            catch (NetworkUnavailableExecption ex)
            {
                return StartupProcedureResult.Fail(
                        ex.UserMessage,
                        canContinue: true);

            }
            catch (ApiUnavailableException)
            {
                return StartupProcedureResult.Fail(
                        "L'API n'est pas joignable. Veuillez vérifier que le serveur est démarré.",
                        canContinue: true);

            }
            catch (Exception ex)
            {
#if DEBUG
                _logger.LogError(ex, "Erreur inattendue lors de la vérification de l'API");
#endif
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
#if DEBUG
            await Task.Delay(500);
#endif

            try
            {
                var hasToken = await VerifyHasToken();
                if (!hasToken)
                {
#if DEBUG
                    _logger.LogInformation("Aucun token, pas de session à valider");
#endif
                    return StartupProcedureResult.Ok(); // Pas d'erreur, juste pas de session
                }

                var isTokenValid = await _secureStorage.IsTokenValidAsync();
                if (!isTokenValid)
                {
#if DEBUG
                    _logger.LogWarning("❌ Token expiré, nettoyage de la session");
#endif
                    await _secureStorage.ClearAllAsync();
                    await _authState.ClearStateAsync();
                    return StartupProcedureResult.Ok(); // Token expiré, redirection vers login
                }

#if DEBUG
                _logger.LogInformation("✅ Token valide localement");
#endif

                //// Essayer de valider via l'API (mode online)

                try
                {
                    // Vérifier d'abord si l'API est joignable
                    await _apiAuthService.CheckApiAvailabilityAsync();

                    // API joignable, valider la session
                    var isValid = await _sessionValidation.ValidateSessionAsync();

                    if (!isValid)
                    {
#if DEBUG
                        _logger.LogWarning("❌ Session invalide selon l'API, nettoyage...");
#endif
                        await _sessionValidation.ClearSessionAsync();
                        await _authState.ClearStateAsync();
                        return StartupProcedureResult.Ok(); // Session invalide, redirection vers login
                    }

                    // Session valide : extraire les infos utilisateur du token JWT
#if DEBUG
                    _logger.LogInformation("✅ Session valide (mode online)");
#endif
                    var userInfo = await _secureStorage.GetUserInfoFromTokenAsync();

                    if (userInfo.HasValue)
                    {
                        // Sauvegarder l'état d'authentification
                        var authState = AuthenticationState.Authenticated(
                            userInfo.Value.UserId,
                            userInfo.Value.Email,
                            userInfo.Value.FirstName,
                            userInfo.Value.LastName
                        );

                        await _authState.SetStateAsync(authState);
#if DEBUG
                        _logger.LogInformation("État d'authentification sauvegardé pour {Email}", userInfo.Value.Email);
#endif
                    }

                    return StartupProcedureResult.Ok();
                }
                catch (ApiUnavailableException ex)
                {
#if DEBUG
                    _logger.LogWarning(ex, "📡 API non joignable, activation du mode offline");
#endif
                    // API non joignable, mais token valide localement -> Mode offline

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
#if DEBUG
                        _logger.LogInformation("✅ Authentification offline réussie pour {Email}", email);
#endif
                        return StartupProcedureResult.Ok();
                    }
                    else
                    {
#if DEBUG
                        _logger.LogWarning("❌ Impossible d'extraire les infos du token");
#endif
                        await _secureStorage.ClearAllAsync();
                        await _authState.ClearStateAsync();
                        return StartupProcedureResult.Fail("Impossible d'extraire les infos du token", canContinue: true);
                    }
                }
            }
            catch (NetworkUnavailableExecption ex)
            {
                return StartupProcedureResult.Fail(
                        ex.UserMessage,
                        canContinue: true);

            }
            catch (Exception ex)
            {
#if DEBUG
                _logger.LogError(ex, "Erreur lors de la validation de session");
#endif
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
#if DEBUG
                _logger.LogInformation("Aucun token, pas de session à valider");
#endif
                return !hasToken; // Pas d'erreur, juste pas de session
            }

#if DEBUG
            _logger.LogInformation("✅ Token valide localement");
#endif
            return hasToken;
        }
    }

}
