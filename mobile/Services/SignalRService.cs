using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace mobile.Services
{
    /// <summary>
    /// Service de gestion des connexions SignalR pour l'application mobile
    /// </summary>
    public class SignalRService : ISignalRService
    {
        private readonly IConfiguration _configuration;
        private readonly ISecureStorageService _secureStorage;
        private readonly ILogger<SignalRService> _logger;

        private HubConnection? _usersHubConnection;
        private HubConnection? _forecastHubConnection;
        private string? _currentEmail;

        public bool IsConnected =>
            (_usersHubConnection?.State == HubConnectionState.Connected) ||
            (_forecastHubConnection?.State == HubConnectionState.Connected);

        // Événements
        public event EventHandler<Models.WeatherForecast>? ForecastCreated;
        public event EventHandler<Models.WeatherForecast>? ForecastUpdated;
        public event EventHandler<int>? ForecastDeleted;
        public event EventHandler<EmailNotification>? EmailSent;
        public event EventHandler<EmailNotification>? VerificationEmailSent;

        public SignalRService(
            IConfiguration configuration,
            ISecureStorageService secureStorage,
            ILogger<SignalRService> logger)
        {
            _configuration = configuration;
            _secureStorage = secureStorage;
            _logger = logger;
        }

        /// <summary>
        /// Démarre la connexion au hub Users
        /// </summary>
        public async Task StartUsersHubAsync(string? email = null)
        {
            try
            {
                if (_usersHubConnection?.State == HubConnectionState.Connected)
                {
                    _logger.LogInformation("UsersHub déjà connecté");
                    return;
                }

                // Nettoyer l'ancienne connexion si elle existe (évite les doubles abonnements)
                if (_usersHubConnection != null)
                {
                    _logger.LogInformation("Nettoyage de l'ancienne connexion UsersHub");
                    await _usersHubConnection.DisposeAsync();
                    _usersHubConnection = null;
                }

                var hubUrl = GetHubUrl("/hubs/users");
                var token = await _secureStorage.GetTokenAsync();

                _logger.LogInformation("Connexion au UsersHub: {HubUrl}", hubUrl);

                _usersHubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        if (!string.IsNullOrEmpty(token))
                        {
                            options.AccessTokenProvider = () => Task.FromResult(token);
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
                    _logger.LogInformation("UsersHub reconnecté: {ConnectionId}", connectionId);

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

                        _logger.LogInformation("📧 MOBILE - Email envoyé: {Subject}", notification.Subject);
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

                        _logger.LogInformation("✅ MOBILE - Email de vérification envoyé: {Message}", notification.Message);
                        VerificationEmailSent?.Invoke(this, notification);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors du traitement de VerificationEmailSentToUser");
                    }
                });

                await _usersHubConnection.StartAsync();
                _logger.LogInformation("✅ MOBILE - UsersHub connecté");

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
        public async Task StartForecastHubAsync()
        {
            try
            {
                if (_forecastHubConnection?.State == HubConnectionState.Connected)
                {
                    _logger.LogInformation("ForecastHub déjà connecté");
                    return;
                }

                // Nettoyer l'ancienne connexion si elle existe (évite les doubles abonnements)
                if (_forecastHubConnection != null)
                {
                    _logger.LogInformation("Nettoyage de l'ancienne connexion ForecastHub");
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

                _logger.LogInformation("Connexion au ForecastHub: {HubUrl}", hubUrl);

                _forecastHubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(token);
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
                    _logger.LogInformation("ForecastHub reconnecté: {ConnectionId}", connectionId);
                    return Task.CompletedTask;
                };

                _forecastHubConnection.Closed += error =>
                {
                    _logger.LogWarning("ForecastHub déconnecté: {Error}", error?.Message);
                    return Task.CompletedTask;
                };

                // Écouter les événements de forecast
                _forecastHubConnection.On<Models.WeatherForecast>("ForecastCreated", (forecast) =>
                {
                    _logger.LogInformation("📢 MOBILE - Forecast créé: ID={Id}", forecast.Id);
                    ForecastCreated?.Invoke(this, forecast);
                });

                _forecastHubConnection.On<Models.WeatherForecast>("ForecastUpdated", (forecast) =>
                {
                    _logger.LogInformation("📢 MOBILE - Forecast mis à jour: ID={Id}", forecast.Id);
                    ForecastUpdated?.Invoke(this, forecast);
                });

                _forecastHubConnection.On<int>("ForecastDeleted", (id) =>
                {
                    _logger.LogInformation("📢 MOBILE - Forecast supprimé: ID={Id}", id);
                    ForecastDeleted?.Invoke(this, id);
                });

                await _forecastHubConnection.StartAsync();
                _logger.LogInformation("✅ MOBILE - ForecastHub connecté");
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
        public async Task StopForecastHubAsync()
        {
            try
            {
                if (_forecastHubConnection != null)
                {
                    await _forecastHubConnection.StopAsync();
                    await _forecastHubConnection.DisposeAsync();
                    _forecastHubConnection = null;
                    _logger.LogInformation("ForecastHub déconnecté");
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
        public async Task StopAllAsync()
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
                    _logger.LogInformation("UsersHub déconnecté");
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
        public async Task JoinEmailChannelAsync(string email)
        {
            try
            {
                if (_usersHubConnection?.State == HubConnectionState.Connected)
                {
                    await _usersHubConnection.InvokeAsync("JoinEmailChannel", email);
                    _currentEmail = email;
                    _logger.LogInformation("Rejoint le canal email: {Email}", email);
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
        public async Task LeaveEmailChannelAsync(string email)
        {
            try
            {
                if (_usersHubConnection?.State == HubConnectionState.Connected)
                {
                    await _usersHubConnection.InvokeAsync("LeaveEmailChannel", email);
                    _currentEmail = null;
                    _logger.LogInformation("Quitté le canal email: {Email}", email);
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
        private string GetHubUrl(string hubPath)
        {
            var baseUrl = "";
#if ANDROID
            baseUrl = _configuration["ApiSettings:BaseUrlDevice"]
                ?? _configuration["ApiSettings:BaseUrlEmulator"];
#elif IOS
            baseUrl = _configuration["ApiSettings:BaseUrlDevice"] 
                ?? _configuration["ApiSettings:BaseUrlEmulator"];
#elif WINDOWS
            baseUrl = _configuration["ApiSettings:BaseUrlWindows"];
#endif

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("ApiSettings:BaseUrl* n'est pas configuré.");
            }

            // Retirer le slash final si présent
            baseUrl = baseUrl.TrimEnd('/');

            return $"{baseUrl}{hubPath}";
        }
    }
}
