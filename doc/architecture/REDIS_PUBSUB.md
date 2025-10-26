# 🚀 Redis Pub/Sub - Communication Inter-Process

## 📋 Architecture

Cette implémentation utilise **Redis Pub/Sub** avec un **EventPublisher custom** pour permettre la communication entre l'API et l'Application Web, afin que les clients Web reçoivent les notifications en temps réel même lorsque les modifications proviennent de l'API.

> **Note :** Ce système utilise un EventPublisher custom pour plus de simplicité et de performance.

### Flux Complet

```
┌──────────────────────────────────────────────────────────────────┐
│                         USER ACTION                               │
└──────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┴───────────────┐
                ▼                               ▼
    ┌─────────────────────┐         ┌─────────────────────┐
    │   API Controller    │         │   Web Controller    │
    │   (REST Endpoint)   │         │   (MVC Action)      │
    └──────────┬──────────┘         └──────────┬──────────┘
               │                               │
               ▼                               ▼
    ┌─────────────────────────────────────────────────────┐
    │        WeatherForecastService (Domain)              │
    │                                                     │
    │  1. Persister en DB                                 │
    │  2. await _publisher.Publish(event)                 │
    └──────────────────────┬──────────────────────────────┘
                           │
                           ▼
               ┌───────────────────────┐
               │   Event Dispatcher    │
               └───────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
┌───────────────┐  ┌──────────────┐  ┌──────────────────┐
│ SignalR       │  │ Audit Log    │  │ Redis Broker     │
│ Handler (Web) │  │ Handler      │  │ Handler (API)    │
│               │  │              │  │                  │
│ Broadcast ✅ │   │ Log ✅      │  │ Publish → Redis  │
└───────────────┘  └──────────────┘  └──────────┬───────┘
                                                 │
                                                 ▼
                                    ┌─────────────────────┐
                                    │   Redis Pub/Sub     │
                                    │                     │
                                    │ Channels:           │
                                    │ - .created          │
                                    │ - .updated          │
                                    │ - .deleted          │
                                    └──────────┬──────────┘
                                               │
                                               ▼
                                    ┌─────────────────────┐
                                    │ RedisSubscriber     │
                                    │ Service (Web)       │
                                    │                     │
                                    │ BackgroundService   │
                                    └──────────┬──────────┘
                                               │
                                               ▼
                                    ┌─────────────────────┐
                                    │   SignalR Hub       │
                                    │   (Web)             │
                                    │                     │
                                    │ Broadcast to Clients│
                                    └──────────┬──────────┘
                                               │
                                               ▼
                                    ┌─────────────────────┐
                                    │   Web Clients       │
                                    │   (Browsers)        │
                                    │                     │
                                    │ Receive real-time   │
                                    │ notifications ✅     │
                                    └─────────────────────┘
```

---

## 🔧 Composants

### **1. API : Redis Broker Handler**

**Fichier** : `api/Handlers/WeatherForecast/RedisBrokerHandler.cs`

**Rôle** : Publier les domain events sur Redis

```csharp
public async Task Handle(ForecastCreatedEvent notification, CancellationToken ct)
{
    var subscriber = _redis.GetSubscriber();
    var message = JsonSerializer.Serialize(notification.Forecast);
    
    await subscriber.PublishAsync("weatherforecast.created", message);
}
```

**Canaux Redis** :
- `weatherforecast.created` - Prévision créée
- `weatherforecast.updated` - Prévision mise à jour
- `weatherforecast.deleted` - Prévision supprimée

---

### **2. Web App : Redis Subscriber Service**

**Fichier** : `application/BackgroundServices/RedisSubscriberService.cs`

**Rôle** : Écouter les events Redis et broadcaster via SignalR

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var subscriber = _redis.GetSubscriber();
    
    // S'abonner aux events
    await subscriber.SubscribeAsync("weatherforecast.created", async (channel, message) =>
    {
        var forecast = JsonSerializer.Deserialize<WeatherForecast>(message);
        await _hubContext.Clients.All.SendAsync("ForecastCreated", forecast);
    });
    
    // ... autres abonnements
}
```

---

## 📊 Scénarios de Fonctionnement

### **Scénario A : Création depuis l'Application Web**

```
User (Web) → Create Forecast
    ↓
Web Controller → Service → EventPublisher
    ↓
┌─────────────────────────────┐
│ SignalRHandler (Web)        │ → Clients Web reçoivent ✅
│ AuditLogHandler (Web)       │ → Logs ✅
└─────────────────────────────┘
```

**Résultat** : Les clients Web reçoivent la notification directement via le SignalRHandler

---

### **Scénario B : Création depuis l'API REST**

```
API Client → POST /api/weatherforecast
    ↓
API Controller → Service → EventPublisher
    ↓
┌─────────────────────────────┐
│ RedisBrokerHandler (API)    │ → Publish Redis
│ AuditLogHandler (API)       │ → Logs ✅
└─────────────────────────────┘
    ↓
Redis Pub/Sub
    ↓
RedisSubscriberService (Web)
    ↓
SignalR Broadcast
    ↓
Clients Web reçoivent ✅
```

**Résultat** : Les clients Web reçoivent la notification via Redis → RedisSubscriber → SignalR

---

## ✅ Avantages de Cette Architecture

### **1. Découplage Total**
- L'API ne connaît pas SignalR
- L'API publie juste sur Redis
- Le Web écoute Redis et broadcaste

### **2. Extensibilité**
```
Redis Pub/Sub peut être écouté par :
- Application Web (SignalR) ✅
- Workers (background jobs)
- Webhooks service
- Analytics service
- Audit service centralisé
- Microservices tiers
```

### **3. Résilience**
- Si le Web est down, l'API fonctionne toujours
- Messages Redis peuvent être persistés (avec AOF)
- Retry automatique de connexion Redis

### **4. Performance**
- Redis Pub/Sub est ultra-rapide (< 1ms)
- Pas de polling
- Push instantané

---

## 🧪 Tests

### **Test 1 : Application Web → Clients Web**

1. Ouvrir 2 navigateurs sur `https://localhost:5001/WeatherForecast`
2. Dans navigateur 1 : Créer une prévision
3. Dans navigateur 2 : **Notification apparaît instantanément** ✅

**Flow** : Web Controller → EventPublisher → SignalRHandler → Clients

---

### **Test 2 : API REST → Clients Web** 🎯

1. Ouvrir navigateur sur `https://localhost:5001/WeatherForecast`
2. Via Postman/Swagger : `POST https://localhost:7252/api/weatherforecast`
3. Dans navigateur : **Notification apparaît instantanément** ✅

**Flow** : API Controller → EventPublisher → RedisBrokerHandler → Redis → RedisSubscriber → SignalR → Clients

---

### **Test 3 : Vérifier les Logs**

**API Logs** :
```
📤 [Redis Pub] Event publié sur canal 'weatherforecast.created' - ID: 123
📋 [API Audit] Forecast Created via API - ID: 123
```

**Web Logs** :
```
🔔 Redis Subscriber Service démarré
✅ Abonné aux canaux Redis: weatherforecast.created, weatherforecast.updated, weatherforecast.deleted
📥 [Redis Sub] Event reçu sur 'weatherforecast.created' - ID: 123 → Broadcasting via SignalR
```

---

## 🔧 Configuration

### **Redis Connection String**

**Development (Local)** :
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

**Production (Docker)** :
```json
{
  "ConnectionStrings": {
    "Redis": "redis:6379"
  }
}
```

### **Redis Configuration Options**

```csharp
var configuration = ConfigurationOptions.Parse(redisConnectionString);
configuration.AbortOnConnectFail = false;  // Ne pas planter si Redis est down
configuration.ConnectTimeout = 5000;       // Timeout connexion
configuration.SyncTimeout = 5000;          // Timeout opérations
```

---

## 🚀 Démarrage

### **1. Démarrer Redis (Docker)**

```powershell
cd c:\Users\Utilisateur\Desktop\Candidatures\Nexton\test
.\scripts\setup-database.ps1
```

Démarre PostgreSQL **ET** Redis ✅

---

### **2. Vérifier que Redis fonctionne**

```powershell
docker ps
# Doit afficher : weatherforecast-redis

docker logs weatherforecast-redis
# Doit afficher : Ready to accept connections
```

---

### **3. Démarrer l'Application Web**

```powershell
cd application
dotnet run
```

**Logs attendus** :
```
🔔 Redis Subscriber Service démarré
✅ Abonné aux canaux Redis: ...
```

---

### **4. Démarrer l'API**

```powershell
cd api
dotnet run
```

---

### **5. Tester**

```bash
# Via Postman/Swagger
POST https://localhost:7252/api/weatherforecast
{
  "date": "2025-10-25",
  "temperatureC": 25,
  "summary": "Warm"
}

# Vérifier dans le navigateur que la notification arrive ✅
```