using domain.Entities;
using Microsoft.AspNetCore.SignalR;
using shared.Hubs;
using StackExchange.Redis;
using System.Text.Json;

namespace api.BackgroundServices
{
    /// <summary>
    /// BackgroundService qui écoute les events Redis publiés par l'Application Web
    /// et les broadcaste via SignalR vers les clients API (mobile) connectés
    /// </summary>
    public class RedisSubscriberService : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IHubContext<WeatherForecastHub> _hubContext;
        private readonly ILogger<RedisSubscriberService> _logger;

        // Noms des canaux Redis
        private const string ChannelForecastCreated = "weatherforecast.created";
        private const string ChannelForecastUpdated = "weatherforecast.updated";
        private const string ChannelForecastDeleted = "weatherforecast.deleted";

        public RedisSubscriberService(
            IConnectionMultiplexer redis,
            IHubContext<WeatherForecastHub> hubContext,
            ILogger<RedisSubscriberService> logger)
        {
            _redis = redis;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔔 API - Redis Subscriber Service démarré");

            // Vérifier que Redis est connecté
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("⚠️ API - Redis non connecté au démarrage. Attente de la connexion...");

                // Attendre un peu que la connexion s'établisse
                await Task.Delay(2000, stoppingToken);

                if (!_redis.IsConnected)
                {
                    _logger.LogError("❌ API - Redis non connecté. Le service ne pourra pas recevoir les events de l'Application Web.");
                    _logger.LogInformation("ℹ️ API - Les notifications depuis l'API fonctionneront toujours via SignalR direct.");
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

                _logger.LogInformation(
                    "✅ API - Abonné aux canaux Redis: {Channels}",
                    string.Join(", ", new[] { ChannelForecastCreated, ChannelForecastUpdated, ChannelForecastDeleted }));

                // Attendre indéfiniment (le service tourne en background)
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API - Erreur dans Redis Subscriber Service");
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
                if (sourceApp == "API")
                {
                    _logger.LogDebug("API - Événement ForecastCreated ignoré (source: API)");
                    return;
                }

                var forecast = JsonSerializer.Deserialize<WeatherForecast>(
                    root.GetProperty("Forecast").GetRawText());

                if (forecast != null)
                {
                    _logger.LogInformation(
                        "📥 API - [Redis Sub] Event reçu de {Source} sur '{Channel}' - ID: {Id} → Broadcasting via SignalR",
                        sourceApp,
                        ChannelForecastCreated,
                        forecast.Id);

                    // Broadcaster via SignalR vers tous les clients connectés à l'API
                    await _hubContext.Clients.All.SendAsync("ForecastCreated", forecast);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API - Erreur lors du traitement de ForecastCreated depuis Redis");
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
                if (sourceApp == "API")
                {
                    _logger.LogDebug("API - Événement ForecastUpdated ignoré (source: API)");
                    return;
                }

                var forecast = JsonSerializer.Deserialize<WeatherForecast>(
                    root.GetProperty("Forecast").GetRawText());

                if (forecast != null)
                {
                    _logger.LogInformation(
                        "📥 API - [Redis Sub] Event reçu de {Source} sur '{Channel}' - ID: {Id} → Broadcasting via SignalR",
                        sourceApp,
                        ChannelForecastUpdated,
                        forecast.Id);

                    await _hubContext.Clients.All.SendAsync("ForecastUpdated", forecast);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API - Erreur lors du traitement de ForecastUpdated depuis Redis");
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
                if (sourceApp == "API")
                {
                    _logger.LogDebug("API - Événement ForecastDeleted ignoré (source: API)");
                    return;
                }

                var id = root.GetProperty("Id").GetInt32();

                _logger.LogInformation(
                    "📥 API - [Redis Sub] Event reçu de {Source} sur '{Channel}' - ID: {Id} → Broadcasting via SignalR",
                    sourceApp,
                    ChannelForecastDeleted,
                    id);

                await _hubContext.Clients.All.SendAsync("ForecastDeleted", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API - Erreur lors du traitement de ForecastDeleted depuis Redis");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 API - Redis Subscriber Service arrêté");
            await base.StopAsync(stoppingToken);
        }
    }
}
