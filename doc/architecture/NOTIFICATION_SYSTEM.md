# 🚀 Système de Notifications Inter-Process - WeatherForecast

---

## 📋 Vue d'Ensemble

Ce système permet la **communication temps réel** entre l'**API REST** et l'**Application Web MVC**, garantissant que tous les clients reçoivent les notifications peu importe l'origine de l'action (API ou Web).

### 🎯 Objectifs

- ✅ **Temps réel** : Notifications instantanées sur tous les clients
- ✅ **Inter-process** : Communication API ↔ Web App
- ✅ **Résilience** : Fonctionnement même si Redis tombe
- ✅ **Performance** : Pas de surcharge inutile
- ✅ **Scalabilité** : Support multi-serveurs

---

## 🏗️ Architecture Globale

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SYSTÈME DE NOTIFICATIONS                          │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐                    ┌─────────────┐                        │
│  │ API REST    │                    │ Web MVC     │                        │
│  │ Controller  │                    │ Controller  │                        │
│  └──────┬──────┘                    └──────┬──────┘                        │
│         │                                  │                               │
│         ▼                                  ▼                               │
│  ┌─────────────────────────────────────────────────────────┐               │
│  │           WeatherForecastService (Domain)               │               │
│  │                                                         │               │
│  │  1. Persister en DB (via Repository)                   │               │
│  │  2. await _publisher.Publish(event)                    │               │
│  └──────────────────────┬──────────────────────────────────┘               │
│                         │                                                  │
│                         ▼                                                  │
│  ┌─────────────────────────────────────────────────────────┐               │
│  │              EventPublisher (Custom)                    │               │
│  │                                                         │               │
│  │  • Créer scope DI pour chaque publish                  │               │
│  │  • Task.WhenAll pour paralléliser les handlers         │               │
│  │  • Logging avec CorrelationId                          │               │
│  │  • Gestion d'erreurs robuste                           │               │
│  └──────────────────────┬──────────────────────────────────┘               │
│                         │                                                  │
│         ┌───────────────┼───────────────┐                                  │
│         ▼               ▼               ▼                                  │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                          │
│  │ SignalR     │ │ Audit Log   │ │ Redis Pub   │                          │
│  │ Handler     │ │ Handler     │ │ Handler     │                          │
│  │ (Web only)  │ │ (Both)      │ │ (API only)  │                          │
│  └──────┬──────┘ └─────────────┘ └──────┬──────┘                          │
│         │                               │                                  │
│         ▼                               ▼                                  │
│  ┌─────────────┐                ┌─────────────┐                           │
│  │ SignalR Hub │                │ Redis       │                           │
│  │ Broadcast   │                │ Pub/Sub     │                           │
│  │ (Local)     │                │             │                           │
│  └─────────────┘                └──────┬──────┘                           │
│                                         │                                  │
│                                         ▼                                  │
│                                  ┌─────────────┐                          │
│                                  │ Redis       │                          │
│                                  │ Subscriber  │                          │
│                                  │ Service     │                          │
│                                  │ (Web)       │                          │
│                                  └──────┬──────┘                          │
│                                         │                                  │
│                                         ▼                                  │
│                                  ┌─────────────┐                          │
│                                  │ SignalR Hub │                          │
│                                  │ Broadcast   │                          │
│                                  │ (Remote)    │                          │
│                                  └─────────────┘                          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Composants Détaillés

### 1. 🎯 EventPublisher Custom

**Fichier :** `shared/Messaging/EventPublisher.cs`

**Rôle :** Equivalent MediatR avec une implémentation sur mesure plus simple et performante.

```csharp
public class EventPublisher : IPublisher
{
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        using var scope = _serviceProvider.CreateScope();  // ✅ Nouveau scope DI
        var handlers = scope.ServiceProvider.GetServices<INotificationHandler<TNotification>>().ToList();

        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        
        var tasks = handlers.Select(async handler =>
        {
            try
            {
                await handler.Handle(notification, cancellationToken);
                _logger.LogInformation("Handled {EventType} with {Handler} in {DurationMs} ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {EventType}");
                // ✅ Ne pas throw pour ne pas bloquer les autres handlers
            }
        });

        await Task.WhenAll(tasks);  // ✅ Parallélisation sécurisée
    }
}
```

**✅ Avantages vs MediatR :**
- **Plus simple** : Pas de complexité inutile
- **Plus rapide** : Moins d'overhead
- **Métriques intégrées** : Logging automatique des performances
- **Résilience** : Un handler qui plante n'arrête pas les autres
- **Debugging** : CorrelationId pour tracer les événements

---

### 2. 📡 Handlers par Application

#### 2.1 Application Web (MVC)

**SignalRForecastNotificationHandler** - `application/Handlers/WeatherForecast/`

```csharp
public class SignalRForecastNotificationHandler : 
    INotificationHandler<ForecastCreatedEvent>,
    INotificationHandler<ForecastUpdatedEvent>,
    INotificationHandler<ForecastDeletedEvent>
{
    public async Task Handle(ForecastCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Exclure l'émetteur du broadcast si ConnectionId fourni
        var clients = string.IsNullOrEmpty(notification.ExcludedConnectionId)
            ? _hubContext.Clients.All
            : _hubContext.Clients.AllExcept(notification.ExcludedConnectionId);

        await clients.SendAsync("ForecastCreated", notification.Forecast, cancellationToken);
    }
}
```

#### 2.2 API REST

**RedisBrokerHandler** - `api/Handlers/WeatherForecast/`

```csharp
public class RedisBrokerHandler : 
    INotificationHandler<ForecastCreatedEvent>,
    INotificationHandler<ForecastUpdatedEvent>,
    INotificationHandler<ForecastDeletedEvent>
{
    private const string ChannelForecastCreated = "weatherforecast.created";
    
    public async Task Handle(ForecastCreatedEvent notification, CancellationToken cancellationToken)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis non connecté. Event non publié");
            return;
        }

        var subscriber = _redis.GetSubscriber();
        var message = JsonSerializer.Serialize(notification.Forecast);

        await subscriber.PublishAsync(
            new RedisChannel(ChannelForecastCreated, RedisChannel.PatternMode.Literal),
            message);
    }
}
```

---

### 3. 🔔 RedisSubscriberService

**Fichier :** `application/BackgroundServices/RedisSubscriberService.cs`

**Rôle :** Écouter les événements Redis de l'API et les broadcaster via SignalR.

```csharp
public class RedisSubscriberService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogError("Redis non connecté. Communication inter-process désactivée.");
            return;
        }

        var subscriber = _redis.GetSubscriber();

        // S'abonner aux événements WeatherForecast
        await subscriber.SubscribeAsync("weatherforecast.created", HandleForecastCreated);
        await subscriber.SubscribeAsync("weatherforecast.updated", HandleForecastUpdated);
        await subscriber.SubscribeAsync("weatherforecast.deleted", HandleForecastDeleted);

        // S'abonner aux événements Admin
        await subscriber.SubscribeAsync("admin.user.registered", HandleAdminUserRegistered);
        // ... autres événements admin

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleForecastCreated(RedisValue message)
    {
        var forecast = JsonSerializer.Deserialize<WeatherForecast>(message.ToString());
        await _hubContext.Clients.All.SendAsync("ForecastCreated", forecast);
    }
}
```

---

## 📊 Scénarios de Fonctionnement

### 🌐 Scénario A : Action depuis l'Application Web

```
👤 User (Web Browser) → Create Forecast
    ↓
🌐 Web MVC Controller → WeatherForecastService
    ↓
📡 EventPublisher.Publish(ForecastCreatedEvent)
    ↓
┌─────────────────────────────────────────┐
│ Handlers exécutés en parallèle :        │
│                                         │
│ ✅ SignalRHandler (Web)                 │ → 📢 Clients Web reçoivent
│ ✅ AuditLogHandler (Web)                │ → 📝 Logs
│                                         │
│ ❌ RedisBrokerHandler (pas enregistré)  │
└─────────────────────────────────────────┘
```

**Résultat :** Les clients Web reçoivent la notification **directement** via SignalR.

---

### 🚀 Scénario B : Action depuis l'API REST

```
📱 API Client → POST /api/weatherforecast
    ↓
🚀 API Controller → WeatherForecastService
    ↓
📡 EventPublisher.Publish(ForecastCreatedEvent)
    ↓
┌─────────────────────────────────────────┐
│ Handlers exécutés en parallèle :        │
│                                         │
│ ✅ RedisBrokerHandler (API)             │ → 📤 Publish Redis
│ ✅ AuditLogHandler (API)                │ → 📝 Logs
│                                         │
│ ❌ SignalRHandler (pas enregistré)      │
└─────────────────────────────────────────┘
    ↓
📮 Redis Pub/Sub : "weatherforecast.created"
    ↓
🔔 RedisSubscriberService (Web) reçoit l'événement
    ↓
📢 SignalR Broadcast vers tous les clients Web
    ↓
👤 Clients Web reçoivent la notification ✅
```

**Résultat :** Les clients Web reçoivent la notification **via Redis** → RedisSubscriber → SignalR.

---

## 🛡️ Gestion d'Erreurs et Résilience

### 1. **Redis Indisponible**

```csharp
if (!_redis.IsConnected)
{
    _logger.LogWarning("Redis non connecté. Event non publié");
    return; // ✅ Pas d'exception, les autres handlers continuent
}
```

**Comportement :**
- ✅ **Web → Web** : Fonctionne toujours (SignalR direct)
- ❌ **API → Web** : Pas de notification (Redis requis)
- ✅ **Logs** : Continuent de fonctionner

### 2. **Handler qui Plante**

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error handling {EventType}");
    // ✅ Ne pas throw pour ne pas bloquer les autres handlers
}
```

**Comportement :**
- ✅ Les autres handlers continuent leur exécution
- ✅ L'erreur est loggée pour debugging
- ✅ L'application reste stable

### 3. **SignalR Indisponible**

```csharp
try
{
    await clients.SendAsync("ForecastCreated", notification.Forecast);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Erreur lors du broadcast SignalR");
    // ✅ Ne pas throw
}
```

**Comportement :**
- ✅ Les autres handlers (Redis, Audit) continuent
- ✅ L'erreur est loggée
- ❌ Les clients Web ne reçoivent pas cette notification

---

## 📋 Canaux Redis Utilisés

### WeatherForecast Events
- `weatherforecast.created` - Nouvelle prévision
- `weatherforecast.updated` - Prévision modifiée
- `weatherforecast.deleted` - Prévision supprimée

### Admin Events
- `admin.user.registered` - Nouvel utilisateur
- `admin.user.loggedin` - Connexion utilisateur
- `admin.user.loggedout` - Déconnexion utilisateur
- `admin.session.created` - Nouvelle session
- `admin.apikey.created` - Nouvelle clé API
- `admin.apikey.revoked` - Clé API révoquée
- `admin.user.rolechanged` - Changement de rôle
- `admin.user.claimchanged` - Changement de permission

---

## 🔍 Debugging et Monitoring

### 1. **Logs Structurés**

```
📤 [Redis Pub] Event publié sur canal 'weatherforecast.created' - ID: 123
📥 [Redis Sub] Event reçu sur 'weatherforecast.created' - ID: 123 → Broadcasting via SignalR
📢 [SignalR] Broadcasting ForecastCreated: ID=123, TriggeredBy=user@example.com
```

### 2. **Métriques de Performance**

```
Published ForecastCreatedEvent to 3 handlers in 45 ms
Handled ForecastCreatedEvent with SignalRHandler in 12 ms
Handled ForecastCreatedEvent with RedisBrokerHandler in 8 ms
Handled ForecastCreatedEvent with AuditLogHandler in 25 ms
```

### 3. **Correlation IDs**

Chaque événement a un `CorrelationId` unique pour tracer son parcours à travers tous les handlers.

---

## 🚀 Avantages de cette Architecture

### ✅ **Performance**
- **Parallélisation** : Tous les handlers s'exécutent en parallèle
- **Pas de surcharge** : Redis utilisé seulement quand nécessaire
- **Scopes DI séparés** : Pas de conflit entre handlers

### ✅ **Résilience**
- **Isolation des erreurs** : Un handler qui plante n'affecte pas les autres
- **Fallback gracieux** : Fonctionne même si Redis tombe
- **Retry automatique** : Redis se reconnecte automatiquement

### ✅ **Simplicité**
- **Pas de MediatR** : Moins de complexité et de dépendances
- **Code explicite** : Facile à comprendre et débugger
- **Configuration simple** : Enregistrement DI standard

### ✅ **Observabilité**
- **Logs détaillés** : Chaque étape est loggée
- **Métriques intégrées** : Performance de chaque handler
- **Correlation IDs** : Traçabilité complète

---

## 🎯 Conclusion

Ce système de notifications offre une **communication temps réel robuste** entre l'API et l'Application Web, avec une architecture **simple**, **performante** et **résiliente**. 