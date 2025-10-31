using domain.Entities;
using Microsoft.AspNetCore.SignalR;
using shared.Hubs;
using StackExchange.Redis;
using System.Text.Json;

namespace application.BackgroundServices
{
    /// <summary>
    /// BackgroundService qui écoute les events Redis publiés par l'API
    /// et les broadcaste via SignalR vers les clients Web connectés
    /// </summary>
    public class RedisSubscriberService : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IHubContext<WeatherForecastHub> _hubContext;
        private readonly IHubContext<AdminHub> _adminHubContext;
        private readonly ILogger<RedisSubscriberService> _logger;

        // Noms des canaux Redis
        private const string ChannelForecastCreated = "weatherforecast.created";
        private const string ChannelForecastUpdated = "weatherforecast.updated";
        private const string ChannelForecastDeleted = "weatherforecast.deleted";

        // Canaux Admin
        private const string ChUserRegistered = "admin.user.registered";
        private const string ChUserLoggedIn = "admin.user.loggedin";
        private const string ChUserLoggedOut = "admin.user.loggedout";
        private const string ChSessionCreated = "admin.session.created";
        private const string ChApiKeyCreated = "admin.apikey.created";
        private const string ChApiKeyRevoked = "admin.apikey.revoked";
        private const string ChUserRoleChanged = "admin.user.rolechanged";
        private const string ChUserClaimChanged = "admin.user.claimchanged";

        public RedisSubscriberService(
            IConnectionMultiplexer redis,
            IHubContext<WeatherForecastHub> hubContext,
            IHubContext<AdminHub> adminHubContext,
            ILogger<RedisSubscriberService> logger)
        {
            _redis = redis;
            _hubContext = hubContext;
            _adminHubContext = adminHubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔔 WEB - Redis Subscriber Service démarré");

            // Vérifier que Redis est connecté
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("⚠️ WEB - Redis non connecté au démarrage. Attente de la connexion...");

                // Attendre un peu que la connexion s'établisse
                await Task.Delay(2000, stoppingToken);

                if (!_redis.IsConnected)
                {
                    _logger.LogError("❌ WEB - Redis non connecté. Le service ne pourra pas recevoir les events de l'API.");
                    _logger.LogInformation("ℹ️ WEB - Les notifications depuis l'Application Web fonctionneront toujours via SignalR direct.");
                    return;
                }
            }

            try
            {
                var subscriber = _redis.GetSubscriber();

                // S'abonner aux events WeatherForecast (création)
                await subscriber.SubscribeAsync(
                    new RedisChannel(ChannelForecastCreated, RedisChannel.PatternMode.Literal),
                    async (channel, message) =>
                    {
                        await HandleForecastCreated(message);
                    });

                // S'abonner aux events WeatherForecast (mise à jour)
                await subscriber.SubscribeAsync(
                    new RedisChannel(ChannelForecastUpdated, RedisChannel.PatternMode.Literal),
                    async (channel, message) =>
                    {
                        await HandleForecastUpdated(message);
                    });

                // S'abonner aux events WeatherForecast (suppression)
                await subscriber.SubscribeAsync(
                    new RedisChannel(ChannelForecastDeleted, RedisChannel.PatternMode.Literal),
                    async (channel, message) =>
                    {
                        await HandleForecastDeleted(message);
                    });

                // S'abonner aux events Admin
                await subscriber.SubscribeAsync(
                    new RedisChannel(ChUserRegistered, RedisChannel.PatternMode.Literal),
                    async (ch, msg) => await HandleAdminUserRegistered(msg));
                await subscriber.SubscribeAsync(new RedisChannel(ChUserLoggedIn, RedisChannel.PatternMode.Literal), async (ch, msg) => await HandleAdminUserLoggedIn(msg));
                await subscriber.SubscribeAsync(new RedisChannel(ChUserLoggedOut, RedisChannel.PatternMode.Literal), async (ch, msg) => await HandleAdminUserLoggedOut(msg));
                await subscriber.SubscribeAsync(new RedisChannel(ChSessionCreated, RedisChannel.PatternMode.Literal), async (ch, msg) => await HandleAdminSessionCreated(msg));
                await subscriber.SubscribeAsync(new RedisChannel(ChApiKeyCreated, RedisChannel.PatternMode.Literal), async (ch, msg) => await HandleAdminApiKeyCreated(msg));
                await subscriber.SubscribeAsync(new RedisChannel(ChApiKeyRevoked, RedisChannel.PatternMode.Literal), async (ch, msg) => await HandleAdminApiKeyRevoked(msg));
                await subscriber.SubscribeAsync(new RedisChannel(ChUserRoleChanged, RedisChannel.PatternMode.Literal), async (ch, msg) => await HandleAdminUserRoleChanged(msg));
                await subscriber.SubscribeAsync(new RedisChannel(ChUserClaimChanged, RedisChannel.PatternMode.Literal), async (ch, msg) => await HandleAdminUserClaimChanged(msg));


                _logger.LogInformation(
                    "✅ WEB - Abonné aux canaux Redis: {Channels}",
                    string.Join(", ", new[] { ChannelForecastCreated, ChannelForecastUpdated, ChannelForecastDeleted, ChUserRegistered, ChUserLoggedIn, ChUserLoggedOut, ChSessionCreated, ChApiKeyCreated, ChApiKeyRevoked, ChUserRoleChanged, ChUserClaimChanged }));

                // Attendre indéfiniment (le service tourne en background)
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WEB - Erreur dans Redis Subscriber Service");
            }
        }

        /// <summary>
        /// Gère l'événement WeatherForecast (création) reçu depuis Redis
        /// </summary>
        private async Task HandleForecastCreated(RedisValue message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message.ToString());
                var root = doc.RootElement;

                // Vérifier la source de l'événement
                var sourceApp = root.TryGetProperty("SourceApp", out var source)
                    ? source.GetString()
                    : "Unknown";

                // Ignorer si c'est notre propre événement
                if (sourceApp == "WEB")
                {
                    _logger.LogDebug("WEB - Événement ForecastCreated ignoré (source: WEB)");
                    return;
                }

                var forecast = JsonSerializer.Deserialize<WeatherForecast>(
                    root.GetProperty("Forecast").GetRawText());

                if (forecast != null)
                {
                    _logger.LogInformation(
                        "📥 WEB - [Redis Sub] Event reçu de {Source} sur '{Channel}' - ID: {Id} → Broadcasting via SignalR",
                        sourceApp,
                        ChannelForecastCreated,
                        forecast.Id);

                    // Broadcaster via SignalR vers tous les clients connectés
                    await _hubContext.Clients.All.SendAsync("ForecastCreated", forecast);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement de ForecastCreated depuis Redis");
            }
        }

        /// <summary>
        /// Gère l'événement WeatherForecast (mise à jour) reçu depuis Redis
        /// </summary>
        private async Task HandleForecastUpdated(RedisValue message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message.ToString());
                var root = doc.RootElement;

                // Vérifier la source de l'événement
                var sourceApp = root.TryGetProperty("SourceApp", out var source)
                    ? source.GetString()
                    : "Unknown";

                // Ignorer si c'est notre propre événement
                if (sourceApp == "WEB")
                {
                    _logger.LogDebug("WEB - Événement ForecastUpdated ignoré (source: WEB)");
                    return;
                }

                var forecast = JsonSerializer.Deserialize<WeatherForecast>(
                    root.GetProperty("Forecast").GetRawText());

                if (forecast != null)
                {
                    _logger.LogInformation(
                        "📥 WEB - [Redis Sub] Event reçu de {Source} sur '{Channel}' - ID: {Id} → Broadcasting via SignalR",
                        sourceApp,
                        ChannelForecastUpdated,
                        forecast.Id);

                    await _hubContext.Clients.All.SendAsync("ForecastUpdated", forecast);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement de ForecastUpdated depuis Redis");
            }
        }

        /// <summary>
        /// Gère l'événement WeatherForecast (suppression) reçu depuis Redis
        /// </summary>
        private async Task HandleForecastDeleted(RedisValue message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message.ToString());
                var root = doc.RootElement;

                // Vérifier la source de l'événement
                var sourceApp = root.TryGetProperty("SourceApp", out var source)
                    ? source.GetString()
                    : "Unknown";

                // Ignorer si c'est notre propre événement
                if (sourceApp == "WEB")
                {
                    _logger.LogDebug("WEB - Événement ForecastDeleted ignoré (source: WEB)");
                    return;
                }

                var id = root.GetProperty("Id").GetInt32();

                _logger.LogInformation(
                    "📥 WEB - [Redis Sub] Event reçu de {Source} sur '{Channel}' - ID: {Id} → Broadcasting via SignalR",
                    sourceApp,
                    ChannelForecastDeleted,
                    id);

                await _hubContext.Clients.All.SendAsync("ForecastDeleted", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement de ForecastDeleted depuis Redis");
            }
        }

        // ==============================================
        // Handlers Admin (réception Redis → AdminHub)
        // ==============================================
        /// <summary>
        /// Gère l'événement d'inscription d'un utilisateur (Admin) reçu depuis Redis
        /// </summary>
        private async Task HandleAdminUserRegistered(RedisValue message)
        {
            try
            {
                var root = JsonSerializer.Deserialize<JsonElement>(message.ToString());
                var data = new
                {
                    userId = root.TryGetProperty("userId", out var c1) ? c1.GetString() : (root.TryGetProperty("UserId", out var p1) ? p1.GetString() : null),
                    email = root.TryGetProperty("email", out var c2) ? c2.GetString() : (root.TryGetProperty("Email", out var p2) ? p2.GetString() : null),
                    userName = root.TryGetProperty("userName", out var c3) ? c3.GetString() : (root.TryGetProperty("UserName", out var p3) ? p3.GetString() : null),
                    registeredAt = root.TryGetProperty("registeredAt", out var c4) ? (c4.ValueKind == JsonValueKind.String ? DateTime.Parse(c4.GetString()!) : c4.GetDateTime()) : (root.TryGetProperty("RegisteredAt", out var p4) ? p4.GetDateTime() : (DateTime?)null),
                    ipAddress = root.TryGetProperty("ipAddress", out var c5) ? c5.GetString() : (root.TryGetProperty("IpAddress", out var p5) ? p5.GetString() : null)
                };
                _logger.LogInformation("📥 [Redis Sub] Admin UserRegistered → Broadcasting via SignalR");
                await _adminHubContext.Clients.All.SendAsync("UserRegistered", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement Admin UserRegistered depuis Redis");
            }
        }

        /// <summary>
        /// Gère l'événement de connexion d'un utilisateur (Admin) reçu depuis Redis
        /// </summary>
        private async Task HandleAdminUserLoggedIn(RedisValue message)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(message.ToString());
                _logger.LogInformation("📥 [Redis Sub] Admin UserLoggedIn → Broadcasting via SignalR");
                await _adminHubContext.Clients.All.SendAsync("UserLoggedIn", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement Admin UserLoggedIn depuis Redis");
            }
        }

        /// <summary>
        /// Gère l'événement de déconnexion d'un utilisateur (Admin) reçu depuis Redis
        /// </summary>
        private async Task HandleAdminUserLoggedOut(RedisValue message)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(message.ToString());
                _logger.LogInformation("📥 [Redis Sub] Admin UserLoggedOut → Broadcasting via SignalR");
                await _adminHubContext.Clients.All.SendAsync("UserLoggedOut", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement Admin UserLoggedOut depuis Redis");
            }
        }

        /// <summary>
        /// Gère l'événement de création de session (Admin) reçu depuis Redis
        /// </summary>
        private async Task HandleAdminSessionCreated(RedisValue message)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(message.ToString());
                _logger.LogInformation("📥 [Redis Sub] Admin SessionCreated → Broadcasting via SignalR");
                await _adminHubContext.Clients.All.SendAsync("SessionCreated", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement Admin SessionCreated depuis Redis");
            }
        }

        /// <summary>
        /// Gère l'événement de création d'API Key (Admin) reçu depuis Redis
        /// </summary>
        private async Task HandleAdminApiKeyCreated(RedisValue message)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(message.ToString());
                _logger.LogInformation("📥 [Redis Sub] Admin ApiKeyCreated → Broadcasting via SignalR");
                await _adminHubContext.Clients.All.SendAsync("ApiKeyCreated", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement Admin ApiKeyCreated depuis Redis");
            }
        }

        /// <summary>
        /// Gère l'événement de révocation d'API Key (Admin) reçu depuis Redis
        /// </summary>
        private async Task HandleAdminApiKeyRevoked(RedisValue message)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(message.ToString());
                _logger.LogInformation("📥 [Redis Sub] Admin ApiKeyRevoked → Broadcasting via SignalR");
                await _adminHubContext.Clients.All.SendAsync("ApiKeyRevoked", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement Admin ApiKeyRevoked depuis Redis");
            }
        }

        /// <summary>
        /// Gère l'événement de changement de rôle (Admin) reçu depuis Redis
        /// </summary>
        private async Task HandleAdminUserRoleChanged(RedisValue message)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(message.ToString());
                _logger.LogInformation("📥 [Redis Sub] Admin UserRoleChanged → Broadcasting via SignalR");
                await _adminHubContext.Clients.All.SendAsync("UserRoleChanged", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement Admin UserRoleChanged depuis Redis");
            }
        }

        /// <summary>
        /// Gère l'événement de changement de claim (Admin) reçu depuis Redis
        /// </summary>
        private async Task HandleAdminUserClaimChanged(RedisValue message)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(message.ToString());
                _logger.LogInformation("📥 [Redis Sub] Admin UserClaimChanged → Broadcasting via SignalR");
                await _adminHubContext.Clients.All.SendAsync("UserClaimChanged", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement Admin UserClaimChanged depuis Redis");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 Redis Subscriber Service arrêté");
            await base.StopAsync(stoppingToken);
        }
    }
}
