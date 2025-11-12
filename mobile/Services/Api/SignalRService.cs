using Microsoft.AspNetCore.SignalR.Client;
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
            ISecureStorageService secureStorage)
        {
            _apiConfig = apiConfig;
            _secureStorage = secureStorage;
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
                    return;
                }

                // Nettoyer l'ancienne connexion si elle existe (évite les doubles abonnements)
                if (_usersHubConnection != null)
                {
                    await _usersHubConnection.DisposeAsync();
                    _usersHubConnection = null;
                }

                var hubUrl = GetHubUrl("/hubs/users");
                var token = await _secureStorage.GetTokenAsync();

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
                    return Task.CompletedTask;
                };

                _usersHubConnection.Reconnected += async connectionId =>
                {
                    // Rejoindre à nouveau le canal email si nécessaire
                    if (!string.IsNullOrEmpty(_currentEmail))
                    {
                        await JoinEmailChannelAsync(_currentEmail);
                    }
                };

                _usersHubConnection.Closed += error =>
                {
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

                        EmailSent?.Invoke(this, notification);
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Shell.Current.DisplayAlert("Debug SignalRService", $"Erreur lors du traitement de EmailSentToUser: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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

                        VerificationEmailSent?.Invoke(this, notification);
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        Shell.Current.DisplayAlert("Debug SignalRService", $"Erreur lors du traitement de VerificationEmailSentToUser: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                    }
                });

                await _usersHubConnection.StartAsync();

                // Rejoindre le canal email si fourni
                if (!string.IsNullOrEmpty(email))
                {
                    await JoinEmailChannelAsync(email);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SignalRService", $"Erreur lors de la connexion au UsersHub: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                    return;
                }

                // Nettoyer l'ancienne connexion si elle existe (évite les doubles abonnements)
                if (_forecastHubConnection != null)
                {
                    await _forecastHubConnection.DisposeAsync();
                    _forecastHubConnection = null;
                }

                var hubUrl = GetHubUrl("/hubs/weatherforecast");
                var token = await _secureStorage.GetTokenAsync();

                if (string.IsNullOrEmpty(token))
                {
                    return;
                }
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
                    return Task.CompletedTask;
                };

                _forecastHubConnection.Reconnected += connectionId =>
                {
                    return Task.CompletedTask;
                };

                _forecastHubConnection.Closed += error =>
                {
                    return Task.CompletedTask;
                };

                // Écouter les événements de forecast
                _forecastHubConnection.On<WeatherForecast>("ForecastCreated", (forecast) =>
                {
                    ForecastCreated?.Invoke(this, forecast);
                });

                _forecastHubConnection.On<WeatherForecast>("ForecastUpdated", (forecast) =>
                {
                    ForecastUpdated?.Invoke(this, forecast);
                });

                _forecastHubConnection.On<int>("ForecastDeleted", (id) =>
                {
                    ForecastDeleted?.Invoke(this, id);
                });

                await _forecastHubConnection.StartAsync();

            }
            catch (Exception ex)
            {
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
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SignalRService", $"Erreur lors de la déconnexion du ForecastHub: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                }

                await StopForecastHubAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SignalRService", $"Erreur lors de la déconnexion de tous les hubs: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SignalRService", $"Erreur lors de la jonction au canal email: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SignalRService", $"Erreur lors de la sortie du canal email: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
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
