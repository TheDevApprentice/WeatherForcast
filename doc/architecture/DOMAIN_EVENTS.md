# 🎯 Domain Events Pattern avec MediatR

## 📋 Architecture

Cette application utilise le **Domain Events Pattern** avec **MediatR** pour implémenter les notifications en temps réel et l'extensibilité du système.

### Structure

```
┌─────────────────────────────────────────────────────────┐
│                    DOMAIN LAYER                         │
│                                                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Events                                           │  │
│  │ - ForecastCreatedEvent                           │  │
│  │ - ForecastUpdatedEvent                           │  │
│  │ - ForecastDeletedEvent                           │  │
│  └──────────────────────────────────────────────────┘  │
│                                                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │ Services                                         │  │
│  │ WeatherForecastService                           │  │
│  │   └─ await _publisher.Publish(event)            │  │
│  └──────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
                        │
                        ▼
            ┌───────────────────────┐
            │      MediatR          │
            │   Event Dispatcher    │
            └───────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        ▼               ▼               ▼
┌───────────────┐ ┌─────────────┐ ┌──────────────┐
│ SignalR       │ │ Audit Log   │ │ Future...    │
│ Handler       │ │ Handler     │ │ (Email, SMS) │
│ (Web + API)   │ │ (Web + API) │ │              │
└───────────────┘ └─────────────┘ └──────────────┘
```

---

## 🚀 Flux d'Exécution

### Exemple : Création d'une prévision météo

#### 1️⃣ **Controller** (Web ou API)
```csharp
// application/Controllers/WeatherForecastController.cs
// api/Controllers/WeatherForecastController.cs

[HttpPost]
public async Task<IActionResult> Create(WeatherForecast forecast)
{
    // Appel du service domain
    await _weatherForecastService.CreateAsync(forecast);
    
    // Pas de code SignalR ici ! ✅
    // Tout est géré automatiquement par les events
    
    return RedirectToAction(nameof(Index));
}
```

#### 2️⃣ **Service Domain**
```csharp
// domain/Services/WeatherForecastService.cs

public async Task<WeatherForecast> CreateAsync(WeatherForecast forecast)
{
    // 1. Persister en base de données
    await _unitOfWork.WeatherForecasts.AddAsync(forecast);
    await _unitOfWork.SaveChangesAsync();
    
    // 2. Publier l'event
    await _publisher.Publish(new ForecastCreatedEvent(forecast));
    
    return forecast;
}
```

#### 3️⃣ **MediatR** dispatche vers tous les handlers

#### 4️⃣ **Handlers** réagissent à l'event

**Handler SignalR** (Application Web uniquement)
```csharp
// application/Handlers/WeatherForecast/SignalRForecastNotificationHandler.cs

public async Task Handle(ForecastCreatedEvent notification, CancellationToken ct)
{
    _logger.LogInformation("Broadcasting ForecastCreated: {Id}", notification.Forecast.Id);
    
    // Broadcast via SignalR vers tous les clients connectés
    await _hubContext.Clients.All
        .SendAsync("ForecastCreated", notification.Forecast, ct);
}
```

**Handler Audit Log** (Web + API)
```csharp
// application/Handlers/WeatherForecast/AuditLogForecastHandler.cs
// api/Handlers/WeatherForecast/ApiAuditLogHandler.cs

public Task Handle(ForecastCreatedEvent notification, CancellationToken ct)
{
    _logger.LogInformation(
        "[Audit] Forecast Created - ID: {Id}, By: {User}",
        notification.Forecast.Id,
        notification.TriggeredBy ?? "System");
    
    // TODO: Persister dans une table d'audit
    return Task.CompletedTask;
}
```

---

## 📦 Fichiers Clés

### Domain Events
```
domain/
  Events/
    WeatherForecast/
      ForecastCreatedEvent.cs    ✅
      ForecastUpdatedEvent.cs    ✅
      ForecastDeletedEvent.cs    ✅
```

### Services
```
domain/
  Services/
    WeatherForecastService.cs   ✅ Publie les events
```

### Handlers (Application Web)
```
application/
  Handlers/
    WeatherForecast/
      SignalRForecastNotificationHandler.cs  ✅ Broadcast SignalR
      AuditLogForecastHandler.cs             ✅ Log audit
```

### Handlers (API)
```
api/
  Handlers/
    WeatherForecast/
      ApiAuditLogHandler.cs                  ✅ Log audit API
```

---

## ✅ Avantages

### 1. **Découplage Total**
- Le service domain ne connaît pas SignalR
- Les controllers ne connaissent pas SignalR
- Facile à tester (mock `IPublisher`)

### 2. **Extensibilité**
Ajouter un nouveau comportement = créer un nouveau handler

**Exemples de handlers possibles** :
- ✅ SignalR notifications (implémenté)
- ✅ Audit logs (implémenté)
- 📧 Email notifications (à implémenter)
- 📱 SMS alerts (à implémenter)
- 🔔 Slack/Discord webhooks (à implémenter)
- 📊 Analytics tracking (à implémenter)

### 3. **Réutilisabilité**
- **Application Web** → Service → Events → Handlers
- **API REST** → Service → Events → Handlers
- **Console App** → Service → Events → Handlers

**Un seul code, plusieurs usages !**

### 4. **Maintenabilité**
- Code organisé et prévisible
- Facile de désactiver un handler (commentaire/config)
- Pas de duplication de code

---

## 🧪 Tests

### Tester le Service
```csharp
[Fact]
public async Task CreateAsync_ShouldPublishEvent()
{
    // Arrange
    var mockPublisher = new Mock<IPublisher>();
    var service = new WeatherForecastService(unitOfWork, mockPublisher.Object);
    
    // Act
    await service.CreateAsync(forecast);
    
    // Assert
    mockPublisher.Verify(p => 
        p.Publish(It.IsAny<ForecastCreatedEvent>(), default), 
        Times.Once);
}
```

### Tester un Handler
```csharp
[Fact]
public async Task Handle_ShouldBroadcastViaSignalR()
{
    // Arrange
    var mockHubContext = new Mock<IHubContext<WeatherForecastHub>>();
    var handler = new SignalRForecastNotificationHandler(mockHubContext.Object, logger);
    
    // Act
    await handler.Handle(new ForecastCreatedEvent(forecast), default);
    
    // Assert
    mockHubContext.Verify(h => 
        h.Clients.All.SendAsync("ForecastCreated", It.IsAny<WeatherForecast>(), default), 
        Times.Once);
}
```

---

## 🔥 Limitations Actuelles

### ⚠️ Notifications depuis l'API vers les Clients Web

**Problème** : L'API et l'Application Web sont 2 processus séparés.

Quand l'API publie un event :
- ✅ Le handler `ApiAuditLogHandler` est déclenché (dans le process API)
- ❌ Le handler `SignalRForecastNotificationHandler` n'est **PAS** déclenché (dans le process Web)

**Résultat** : Les clients Web **ne reçoivent pas** les notifications depuis l'API.

### 🛠️ Solutions (Production)

#### **Option 1 : Redis Backplane pour SignalR** ⭐
```csharp
// Dans Program.cs (application + api)
builder.Services.AddSignalR()
    .AddStackExchangeRedis(configuration.GetConnectionString("Redis"));
```

#### **Option 2 : Message Broker (RabbitMQ, Azure Service Bus)**
```
API → Publish Event → RabbitMQ → Worker consumes → SignalR broadcast
```

#### **Option 3 : Merger API et Web dans le même process**
```
Une seule application avec :
- Controllers Web (MVC)
- Controllers API (REST)
- Un seul SignalR Hub
```

---

## 🎯 Démo Actuelle

**Fonctionne** ✅ :
- Application Web → CRUD → SignalR notifications → Clients Web

**Ne fonctionne pas** ❌ :
- API REST → CRUD → SignalR notifications → Clients Web
  (mais l'audit log fonctionne !)

**Pour tester les notifications temps réel** :
1. Ouvrir 2 navigateurs sur l'application Web (`https://localhost:5001/WeatherForecast`)
2. Créer/Modifier/Supprimer une prévision dans un navigateur
3. Voir la mise à jour en temps réel dans l'autre navigateur ✅

---

## 📚 Ressources

- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [SignalR with Redis Backplane](https://learn.microsoft.com/en-us/aspnet/core/signalr/redis-backplane)
- [Domain Events Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)

---

## 🎉 Résumé

**Domain Events avec MediatR** = Architecture propre, découplée et extensible

- ✅ Services domain ne dépendent pas de l'infrastructure
- ✅ Facile d'ajouter des comportements (handlers)
- ✅ Testable et maintenable
- ✅ Pattern recommandé pour les applications enterprise

**Prêt pour la production avec** :
- Redis Backplane (SignalR multi-instances)
- Message Broker (communication inter-services)
- Monitoring et observabilité
