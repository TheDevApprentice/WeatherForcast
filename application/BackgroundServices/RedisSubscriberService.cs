using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;
using application.Hubs;
using domain.Entities;

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
            _logger.LogInformation("🔔 Redis Subscriber Service démarré");

            // Vérifier que Redis est connecté
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("⚠️ Redis non connecté au démarrage. Attente de la connexion...");
                
                // Attendre un peu que la connexion s'établisse
                await Task.Delay(2000, stoppingToken);
                
                if (!_redis.IsConnected)
                {
                    _logger.LogError("❌ Redis non connecté. Le service ne pourra pas recevoir les events de l'API.");
                    _logger.LogInformation("ℹ️ Les notifications depuis l'Application Web fonctionneront toujours via SignalR direct.");
                    return;
                }
            }

            try
            {
                var subscriber = _redis.GetSubscriber();

                // S'abonner aux events de création
                await subscriber.SubscribeAsync(
                    new RedisChannel(ChannelForecastCreated, RedisChannel.PatternMode.Literal),
                    async (channel, message) =>
                    {
                        await HandleForecastCreated(message);
                    });

                // S'abonner aux events de mise à jour
                await subscriber.SubscribeAsync(
                    new RedisChannel(ChannelForecastUpdated, RedisChannel.PatternMode.Literal),
                    async (channel, message) =>
                    {
                        await HandleForecastUpdated(message);
                    });

                // S'abonner aux events de suppression
                await subscriber.SubscribeAsync(
                    new RedisChannel(ChannelForecastDeleted, RedisChannel.PatternMode.Literal),
                    async (channel, message) =>
                    {
                        await HandleForecastDeleted(message);
                    });

                _logger.LogInformation(
                    "✅ Abonné aux canaux Redis: {Channels}",
                    string.Join(", ", ChannelForecastCreated, ChannelForecastUpdated, ChannelForecastDeleted));

                // Attendre indéfiniment (le service tourne en background)
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur dans Redis Subscriber Service");
            }
        }

        /// <summary>
        /// Gère l'event de création reçu depuis Redis
        /// </summary>
        private async Task HandleForecastCreated(RedisValue message)
        {
            try
            {
                var forecast = JsonSerializer.Deserialize<WeatherForecast>(message.ToString());

                if (forecast != null)
                {
                    _logger.LogInformation(
                        "📥 [Redis Sub] Event reçu sur '{Channel}' - ID: {Id} → Broadcasting via SignalR",
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
        /// Gère l'event de mise à jour reçu depuis Redis
        /// </summary>
        private async Task HandleForecastUpdated(RedisValue message)
        {
            try
            {
                var forecast = JsonSerializer.Deserialize<WeatherForecast>(message.ToString());

                if (forecast != null)
                {
                    _logger.LogInformation(
                        "📥 [Redis Sub] Event reçu sur '{Channel}' - ID: {Id} → Broadcasting via SignalR",
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
        /// Gère l'event de suppression reçu depuis Redis
        /// </summary>
        private async Task HandleForecastDeleted(RedisValue message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message.ToString());
                var id = doc.RootElement.GetProperty("Id").GetInt32();

                _logger.LogInformation(
                    "📥 [Redis Sub] Event reçu sur '{Channel}' - ID: {Id} → Broadcasting via SignalR",
                    ChannelForecastDeleted,
                    id);

                await _hubContext.Clients.All.SendAsync("ForecastDeleted", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement de ForecastDeleted depuis Redis");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 Redis Subscriber Service arrêté");
            await base.StopAsync(stoppingToken);
        }
    }
}
