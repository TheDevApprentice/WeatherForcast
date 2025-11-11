using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using mobile.Services.Api.Interfaces;
using mobile.Services.Internal.Interfaces;
using System.Text.Json;

namespace mobile.Services.Api
{
    /// <summary>
    /// Service de gestion des connexions SignalR pour l'application mobile
    /// </summary>
    public class SignalRService : ISignalRService
    {
        private readonly IApiConfigurationService _apiConfig;
        private readonly ISecureStorageService _secureStorage;
        private readonly ILogger<SignalRService> _logger;

        private HubConnection? _usersHubConnection;
        private HubConnection? _forecastHubConnection;
        private string? _currentEmail;

        public bool IsConnected =>
            _usersHubConnection?.State == HubConnectionState.Connected ||
            _forecastHubConnection?.State == HubConnectionState.Connected;

        // Événements
        public event EventHandler<WeatherForecast>? ForecastCreated;
        public event EventHandler<WeatherForecast>? ForecastUpdated;
        public event EventHandler<int>? ForecastDeleted;
        public event EventHandler<EmailNotification>? EmailSent;
        public event EventHandler<EmailNotification>? VerificationEmailSent;

        public SignalRService (
            IApiConfigurationService apiConfig,
            ISecureStorageService secureStorage,
            ILogger<SignalRService> logger)
        {
            _apiConfig = apiConfig;
            _secureStorage = secureStorage;
            _logger = logger;
        }

        /// <summary>
        /// Démarre la connexion au hub Users
        /// </summary>
        public async Task StartUsersHubAsync (string? email = null)
        {
            try
            {
                if (_usersHubConnection?.State == HubConnectionState.Connected)
                {
#if DEBUG
                    _logger.LogDebug("UsersHub déjà connecté");
#endif
                    return;
                }

                // Nettoyer l'ancienne connexion si elle existe (évite les doubles abonnements)
                if (_usersHubConnection != null)
                {
#if DEBUG
                    _logger.LogDebug("Nettoyage de l'ancienne connexion UsersHub");
#endif
                    await _usersHubConnection.DisposeAsync();
                    _usersHubConnection = null;
                }

                var hubUrl = GetHubUrl("/hubs/users");
                var token = await _secureStorage.GetTokenAsync();

#if DEBUG
                _logger.LogDebug("Connexion au UsersHub: {HubUrl}", hubUrl);
#endif

                _usersHubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        if (!string.IsNullOrEmpty(token))
                        {
                            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                        }
                    })
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                    .Build();

                // Gestion de la reconnexion
                _usersHubConnection.Reconnecting += error =>
                {
                    _logger.LogWarning("UsersHub reconnexion en cours...");
                    return Task.CompletedTask;
                };

                _usersHubConnection.Reconnected += async connectionId =>
                {
#if DEBUG
                    _logger.LogDebug("UsersHub reconnecté: {ConnectionId}", connectionId);
#endif

                    // Rejoindre à nouveau le canal email si nécessaire
                    if (!string.IsNullOrEmpty(_currentEmail))
                    {
                        await JoinEmailChannelAsync(_currentEmail);
                    }
                };

                _usersHubConnection.Closed += error =>
                {
                    _logger.LogWarning("UsersHub déconnecté: {Error}", error?.Message);
                    return Task.CompletedTask;
                };

                // Écouter les événements d'email
                _usersHubConnection.On<JsonElement>("EmailSentToUser", (data) =>
                {
                    try
                    {
                        var notification = new EmailNotification
                        {
                            Subject = data.TryGetProperty("Subject", out var subject) ? subject.GetString() : null,
                            CorrelationId = data.TryGetProperty("CorrelationId", out var corrId) ? corrId.GetString() : null
                        };

#if DEBUG
                        _logger.LogDebug("📧 MOBILE - Email envoyé: {Subject}", notification.Subject);
#endif
                        EmailSent?.Invoke(this, notification);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors du traitement de EmailSentToUser");
                    }
                });

                _usersHubConnection.On<JsonElement>("VerificationEmailSentToUser", (data) =>
                {
                    try
                    {
                        var notification = new EmailNotification
                        {
                            Message = data.TryGetProperty("Message", out var message) ? message.GetString() : null,
                            CorrelationId = data.TryGetProperty("CorrelationId", out var corrId) ? corrId.GetString() : null
                        };

#if DEBUG
                        _logger.LogDebug("✅ MOBILE - Email de vérification envoyé: {Message}", notification.Message);
#endif
                        VerificationEmailSent?.Invoke(this, notification);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors du traitement de VerificationEmailSentToUser");
                    }
                });

                await _usersHubConnection.StartAsync();

#if DEBUG
                _logger.LogDebug("✅ MOBILE - UsersHub connecté");
#endif

                // Rejoindre le canal email si fourni
                if (!string.IsNullOrEmpty(email))
                {
                    await JoinEmailChannelAsync(email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion au UsersHub");
                throw;
            }
        }

        /// <summary>
        /// Démarre la connexion au hub WeatherForecast
        /// </summary>
        public async Task StartForecastHubAsync ()
        {
            try
            {
                if (_forecastHubConnection?.State == HubConnectionState.Connected)
                {
#if DEBUG
                    _logger.LogDebug("ForecastHub déjà connecté");
#endif
                    return;
                }

                // Nettoyer l'ancienne connexion si elle existe (évite les doubles abonnements)
                if (_forecastHubConnection != null)
                {
#if DEBUG
                    _logger.LogDebug("Nettoyage de l'ancienne connexion ForecastHub");
#endif
                    await _forecastHubConnection.DisposeAsync();
                    _forecastHubConnection = null;
                }

                var hubUrl = GetHubUrl("/hubs/weatherforecast");
                var token = await _secureStorage.GetTokenAsync();

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Pas de token JWT, connexion au ForecastHub impossible");
                    return;
                }

#if DEBUG
                _logger.LogDebug("Connexion au ForecastHub: {HubUrl}", hubUrl);
#endif

                _forecastHubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    })
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                    .Build();

                // Gestion de la reconnexion
                _forecastHubConnection.Reconnecting += error =>
                {
                    _logger.LogWarning("ForecastHub reconnexion en cours...");
                    return Task.CompletedTask;
                };

                _forecastHubConnection.Reconnected += connectionId =>
                {
#if DEBUG
                    _logger.LogDebug("ForecastHub reconnecté: {ConnectionId}", connectionId);
#endif
                    return Task.CompletedTask;
                };

                _forecastHubConnection.Closed += error =>
                {
                    _logger.LogWarning("ForecastHub déconnecté: {Error}", error?.Message);
                    return Task.CompletedTask;
                };

                // Écouter les événements de forecast
                _forecastHubConnection.On<WeatherForecast>("ForecastCreated", (forecast) =>
                {
#if DEBUG
                    _logger.LogDebug("📢 MOBILE - Forecast créé: ID={Id}", forecast.Id);
#endif
                    ForecastCreated?.Invoke(this, forecast);
                });

                _forecastHubConnection.On<WeatherForecast>("ForecastUpdated", (forecast) =>
                {
#if DEBUG
                    _logger.LogDebug("📢 MOBILE - Forecast mis à jour: ID={Id}", forecast.Id);
#endif
                    ForecastUpdated?.Invoke(this, forecast);
                });

                _forecastHubConnection.On<int>("ForecastDeleted", (id) =>
                {
#if DEBUG
                    _logger.LogDebug("📢 MOBILE - Forecast supprimé: ID={Id}", id);
#endif
                    ForecastDeleted?.Invoke(this, id);
                });

                await _forecastHubConnection.StartAsync();

#if DEBUG
                _logger.LogDebug("✅ MOBILE - ForecastHub connecté");
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion au ForecastHub");
                throw;
            }
        }

        /// <summary>
        /// Arrête la connexion au hub WeatherForecast
        /// </summary>
        public async Task StopForecastHubAsync ()
        {
            try
            {
                if (_forecastHubConnection != null)
                {
                    await _forecastHubConnection.StopAsync();
                    await _forecastHubConnection.DisposeAsync();
                    _forecastHubConnection = null;

#if DEBUG
                    _logger.LogDebug("ForecastHub déconnecté");
#endif
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion du ForecastHub");
            }
        }

        /// <summary>
        /// Arrête toutes les connexions SignalR
        /// </summary>
        public async Task StopAllAsync ()
        {
            try
            {
                // Quitter le canal email si nécessaire
                if (!string.IsNullOrEmpty(_currentEmail))
                {
                    await LeaveEmailChannelAsync(_currentEmail);
                }

                if (_usersHubConnection != null)
                {
                    await _usersHubConnection.StopAsync();
                    await _usersHubConnection.DisposeAsync();
                    _usersHubConnection = null;

#if DEBUG
                    _logger.LogDebug("UsersHub déconnecté");
#endif
                }

                await StopForecastHubAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion de tous les hubs");
            }
        }

        /// <summary>
        /// Rejoint le canal email (pour les utilisateurs non authentifiés)
        /// </summary>
        public async Task JoinEmailChannelAsync (string email)
        {
            try
            {
                if (_usersHubConnection?.State == HubConnectionState.Connected)
                {
                    await _usersHubConnection.InvokeAsync("JoinEmailChannel", email);
                    _currentEmail = email;

#if DEBUG
                    _logger.LogDebug("Rejoint le canal email: {Email}", email);
#endif
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la jonction au canal email");
            }
        }

        /// <summary>
        /// Quitte le canal email
        /// </summary>
        public async Task LeaveEmailChannelAsync (string email)
        {
            try
            {
                if (_usersHubConnection?.State == HubConnectionState.Connected)
                {
                    await _usersHubConnection.InvokeAsync("LeaveEmailChannel", email);
                    _currentEmail = null;

#if DEBUG
                    _logger.LogDebug("Quitté le canal email: {Email}", email);
#endif
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sortie du canal email");
            }
        }

        /// <summary>
        /// Construit l'URL du hub SignalR
        /// </summary>
        private string GetHubUrl (string hubPath)
        {
            // Utiliser le service de configuration centralisé
            return _apiConfig.GetHubUrl(hubPath);
        }
    }
}
