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
        private readonly IApiAuthService _apiAuthService;
        private readonly ISessionValidationService _sessionValidation;
        private readonly ISecureStorageService _secureStorage;
        private readonly IAuthenticationStateService _authState;
        private readonly List<StartupProcedure> _procedures;
        private readonly INetworkMonitorService _networkMonitor;

        public IReadOnlyList<StartupProcedure> Procedures => _procedures.AsReadOnly();

        public StartupService (
            IApiAuthService apiAuthService,
            ISessionValidationService sessionValidation,
            IServiceProvider serviceProvider,
            ISecureStorageService secureStorage,
            IAuthenticationStateService authState,
            INetworkMonitorService networkMonitor)
        {
            _secureStorage = secureStorage;
            _authState = authState;
            _apiAuthService = apiAuthService;
            _sessionValidation = sessionValidation;

            // Résoudre les services via ServiceProvider pour éviter les problèmes de lifetime
            using var scope = serviceProvider.CreateScope();

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
            // Début des procédures de démarrage

            foreach (var procedure in _procedures)
            {
                try
                {
                    // Mettre à jour le statut : Running
                    procedure.Status = StartupProcedureStatus.Running;
                    progress?.Report(procedure);

                    // Exécution de la procédure

                    // Exécuter la procédure
                    var result = await procedure.ExecuteAsync();

                    if (result.Success)
                    {
                        procedure.Status = StartupProcedureStatus.Success;
                        // Procédure réussie
                    }
                    else
                    {
                        procedure.Status = StartupProcedureStatus.Failed;
                        procedure.ErrorMessage = result.ErrorMessage;
                        // Procédure échouée

                        progress?.Report(procedure);

                        // Si on ne peut pas continuer, arrêter la queue
                        if (!result.CanContinue)
                        {
                            // Arrêt des procédures de démarrage
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
                    await Shell.Current.DisplayAlert("Debug StartupService", $"❌ Erreur lors de l'exécution de {procedure.Name}: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif

                    progress?.Report(procedure);
                    return false;
                }
            }

            // Toutes les procédures de démarrage terminées
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
                    // Réseau indisponible
                    throw new NetworkUnavailableExecption();
                }

                // Réseau disponible
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
                await Shell.Current.DisplayAlert("Debug StartupService", $"❌ Erreur lors de la vérification du réseau: {ex.Message}\n{ex.GetType().Name}", "OK");
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
            await Task.Delay(500);

            try
            {
                // Essayer de valider via l'API (mode online)
                await _apiAuthService.CheckApiAvailabilityAsync();

                // Si on arrive ici sans exception, l'API est joignable
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
                await Shell.Current.DisplayAlert("Debug StartupService", $"❌ Erreur inattendue lors de la vérification de l'API: {ex.Message}\n{ex.GetType().Name}", "OK");
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
                    // Aucun token, pas de session à valider
                    return StartupProcedureResult.Ok(); // Pas d'erreur, juste pas de session
                }

                var isTokenValid = await _secureStorage.IsTokenValidAsync();
                if (!isTokenValid)
                {
                    // Token expiré, nettoyage de la session
                    await _secureStorage.ClearAllAsync();
                    await _authState.ClearStateAsync();
                    return StartupProcedureResult.Ok(); // Pas d'erreur, juste pas de session
                }

                // Token valide localement

                //// Essayer de valider via l'API (mode online)

                try
                {
                    // Vérifier d'abord si l'API est joignable
                    await _apiAuthService.CheckApiAvailabilityAsync();

                    // API joignable, valider la session
                    var isValid = await _sessionValidation.ValidateSessionAsync();

                    if (!isValid)
                    {
                        // Session invalide selon l'API, nettoyage
                        await _sessionValidation.ClearSessionAsync();
                        await _authState.ClearStateAsync();
                        return StartupProcedureResult.Ok(); // Session invalide, redirection vers login
                    }

                    // Session valide : extraire les infos utilisateur du token JWT
                    // Session valide (mode online)
                    var userId = await _secureStorage.GetUserIdFromTokenAsync();
                    var userInfo = await _secureStorage.GetUserInfoAsync();

                    if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userInfo.Email) && !string.IsNullOrEmpty(userInfo.FirstName) && !string.IsNullOrEmpty(userInfo.LastName))
                    {
                        // Sauvegarder l'état d'authentification
                        var authState = AuthenticationState.Authenticated(
                            userId,
                            userInfo.Email,
                            userInfo.FirstName,
                            userInfo.LastName
                        );

                        await _authState.SetStateAsync(authState);
                        // État d'authentification sauvegardé
                    }

                    return StartupProcedureResult.Ok();
                }
                catch (ApiUnavailableException ex)
                {
                    // API non joignable, activation du mode offline
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
                        // Authentification offline réussie
                        return StartupProcedureResult.Ok();
                    }
                    else
                    {
                        // Impossible d'extraire les infos du token
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
                await Shell.Current.DisplayAlert("Debug StartupService", $"❌ Erreur lors de la validation de session: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                // Pas de token, pas besoin de valider
                return true; // Pas d'erreur, juste pas de session
            }

            // Token valide localement
            return hasToken;
        }
    }
}
