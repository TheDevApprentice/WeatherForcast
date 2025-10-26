# 🛡️ Gestion des Erreurs - Documentation Complète

## 📋 Table des Matières

- [Vue d'Ensemble](#-vue-densemble)
- [Architecture](#-architecture)
- [Exceptions Typées](#-exceptions-typées)
- [Middleware Global](#-middleware-global)
- [Gestion dans les Controllers](#-gestion-dans-les-controllers)
- [Notifications Temps Réel](#-notifications-temps-réel)
- [Bufferisation Redis](#-bufferisation-redis)
- [AJAX et UX](#-ajax-et-ux)
- [Flux Complets](#-flux-complets)
- [Tests et Validation](#-tests-et-validation)

---

## 🎯 Vue d'Ensemble

Le système de gestion d'erreurs implémente une architecture complète avec :

- ✅ **FluentValidation** pour validation déclarative
- ✅ **Exceptions typées** dans le domain (SOLID, DDD)
- ✅ **Middleware global** comme filet de sécurité
- ✅ **Notifications SignalR** temps réel
- ✅ **Bufferisation Redis** intelligente
- ✅ **AJAX** pour UX fluide sans rechargement
- ✅ **Logs structurés** pour audit et monitoring

### Principes Clés

1. **Séparation des Responsabilités**
   - Domain : Définit les exceptions métier
   - Application : Gère les erreurs et notifie
   - Infrastructure : Persiste les logs d'audit

2. **Fail-Fast**
   - Validation FluentValidation au niveau présentation
   - Validation au plus tôt (constructeurs, Value Objects)
   - Exceptions typées pour erreurs métier
   - Pas de valeurs nulles silencieuses

3. **Observabilité**
   - Logs structurés avec contexte complet
   - Notifications temps réel pour l'utilisateur
   - Audit trail pour investigation

---

## 🏗️ Architecture

### Diagramme de Flux

```
┌─────────────────────────────────────────────────────────────┐
│                    USER ACTION                               │
│              (Create/Update/Delete)                          │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    CONTROLLER                                │
│  • FluentValidation via ModelState.IsValid                  │
│  • Try/Catch avec types spécifiques                         │
│  • Décision: return View() ou RedirectToAction()            │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    DOMAIN SERVICE                            │
│  • Validation métier                                        │
│  • throw ValidationException                                │
│  • throw EntityNotFoundException                            │
│  • throw DatabaseException                                  │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    EVENT PUBLISHER                           │
│  • PublishDomainExceptionAsync()                            │
│  • ErrorOccurredEvent                                       │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    EVENT HANDLERS (Parallèles)               │
│  1. SignalRErrorHandler → Notification temps réel           │
│  2. AuditLogErrorHandler → Logs structurés                  │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    SIGNALR HUB                               │
│  • UsersHub.Clients.User(userId)                            │
│  • SendAsync("ErrorOccurred", payload)                      │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    CLIENT JAVASCRIPT                         │
│  • Reçoit "ErrorOccurred"                                   │
│  • Déduplication (CorrelationId)                            │
│  • showNotification(title, message, "danger")               │
└─────────────────────────────────────────────────────────────┘
```

---

## ✅ FluentValidation - Validation Déclarative

### Vue d'Ensemble

FluentValidation gère la validation au niveau **présentation** (ViewModels/DTOs) avant que les données n'atteignent le domain.

### Configuration

```csharp
// application/Program.cs & api/Program.cs
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters(); // Application uniquement
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

### Pattern dans les Controllers

```csharp
[HttpPost]
public async Task<IActionResult> Create(WeatherForecastViewModel viewModel)
{
    // ✅ Validation FluentValidation via ModelState
    if (!ModelState.IsValid)
    {
        // Publier l'erreur pour notification SignalR
        var errors = string.Join(", ", ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));

        await _publisher.PublishValidationErrorAsync(
            User,
            errors,
            "Create",
            "WeatherForecast",
            null,
            null);

        return View(viewModel);
    }

    // ... logique métier
}
```

### Validators Implémentés

- **Application** : 5 validators (WeatherForecast, ApiKey, Register, Login, CreateUser)
- **API** : 5 validators (Create/Update WeatherForecast, Register, Login)

**Voir** : [`FLUENTVALIDATION.md`](FLUENTVALIDATION.md) pour la documentation complète.

---

## 🎯 Exceptions Typées

### Hiérarchie

```
DomainException (abstract)
├── ValidationException
├── EntityNotFoundException
├── DatabaseException
└── ExternalServiceException
```

### ErrorType Enum

```csharp
// domain/ValueObjects/ErrorType.cs
public enum ErrorType
{
    Validation,      // Erreur de validation des données
    Database,        // Erreur de base de données
    External,        // Erreur d'un service externe
    Authorization,   // Erreur d'autorisation
    NotFound,        // Entité introuvable
    Unknown          // Erreur inconnue
}
```

### DomainException (Base)

```csharp
// domain/Exceptions/DomainException.cs
public abstract class DomainException : Exception
{
    public abstract ErrorType ErrorType { get; }
    public string Action { get; }
    public string EntityType { get; }
    public string? EntityId { get; }

    protected DomainException(
        string message,
        string action,
        string entityType,
        string? entityId,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
    }
}
```

### ValidationException

```csharp
// domain/Exceptions/ValidationException.cs
public class ValidationException : DomainException
{
    public override ErrorType ErrorType => ErrorType.Validation;

    public ValidationException(
        string message,
        string action,
        string entityType,
        string? entityId)
        : base(message, action, entityType, entityId)
    {
    }
}
```

**Utilisation** :

```csharp
// domain/Entities/WeatherForecast.cs
private static void ValidateSummary(string? summary)
{
    var invalidSummaries = new[] { "-- Sélectionnez --", "" };

    if (string.IsNullOrWhiteSpace(summary) || 
        invalidSummaries.Contains(summary.Trim(), StringComparer.OrdinalIgnoreCase))
    {
        throw new ValidationException(
            "Veuillez sélectionner un résumé météo valide.",
            "Validation",
            "WeatherForecast",
            null);
    }
}
```

### EntityNotFoundException

```csharp
// domain/Exceptions/EntityNotFoundException.cs
public class EntityNotFoundException : DomainException
{
    public override ErrorType ErrorType => ErrorType.NotFound;

    public EntityNotFoundException(
        string entityType,
        string entityId,
        string action)
        : base(
            $"{entityType} avec l'ID '{entityId}' est introuvable.",
            action,
            entityType,
            entityId)
    {
    }
}
```

**Utilisation** :

```csharp
// domain/Services/ApiKeyService.cs
public async Task<bool> RevokeApiKeyAsync(int apiKeyId, string userId, string reason)
{
    var apiKey = await _unitOfWork.ApiKeys.GetByIdAsync(apiKeyId);

    if (apiKey == null)
    {
        throw new EntityNotFoundException("ApiKey", apiKeyId.ToString(), "Revoke");
    }
    
    // ...
}
```

### DatabaseException

```csharp
// domain/Exceptions/DatabaseException.cs
public class DatabaseException : DomainException
{
    public override ErrorType ErrorType => ErrorType.Database;

    public DatabaseException(
        string message,
        string action,
        string entityType,
        string? entityId,
        Exception? innerException = null)
        : base(message, action, entityType, entityId, innerException)
    {
    }
}
```

**Utilisation** :

```csharp
// domain/Services/ApiKeyService.cs
try
{
    await _unitOfWork.ApiKeys.CreateAsync(apiKey);
    await _unitOfWork.SaveChangesAsync();
}
catch (Exception ex) when (ex is not DomainException)
{
    throw new DatabaseException(
        "Erreur lors de la création de la clé API.",
        "Create",
        "ApiKey",
        null,
        ex);
}
```

---

## 🛡️ Middleware Global

### GlobalErrorHandlerMiddleware

**Rôle** : Filet de sécurité pour catcher les exceptions **non gérées** dans les controllers.

```csharp
// application/Middleware/GlobalErrorHandlerMiddleware.cs
public class GlobalErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalErrorHandlerMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context, IPublisher publisher)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            // ✅ Exception typée du domain - déjà gérée normalement
            _logger.LogWarning(ex, 
                "[GlobalErrorHandler] DomainException non catchée | Type={ErrorType} | Action={Action}",
                ex.ErrorType,
                ex.Action);

            // Publication commentée (optionnel)
            // var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            // if (!string.IsNullOrEmpty(userId))
            // {
            //     await publisher.PublishDomainExceptionAsync(context.User, ex);
            // }

            context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(ex.Message)}");
        }
        catch (Exception ex)
        {
            // ❌ Exception non gérée - Erreur critique
            _logger.LogError(ex, 
                "[GlobalErrorHandler] Exception non gérée | Path={Path} | User={User}",
                context.Request.Path,
                context.User?.Identity?.Name ?? "Anonymous");

            context.Response.Redirect("/Home/Error");
        }
    }
}
```

### Enregistrement

```csharp
// application/Program.cs
// ✅ Middleware global d'erreurs (filet de sécurité)
app.UseGlobalErrorHandler();
```

### Quand le Middleware est Exécuté ?

| Scénario | Middleware Exécuté ? |
|----------|---------------------|
| Exception catchée dans le controller | ❌ Non |
| Exception non catchée dans le controller | ✅ Oui |
| NullReferenceException (bug) | ✅ Oui |
| Exception dans un middleware précédent | ✅ Oui |

---

## 🎮 Gestion dans les Controllers

### Pattern Standard

```csharp
public async Task<IActionResult> Create(WeatherForecastViewModel viewModel)
{
    if (ModelState.IsValid)
    {
        try
        {
            // 1. Appeler le service
            var temperature = new Temperature(viewModel.TemperatureC);
            var forecast = new WeatherForecast(viewModel.Date, temperature, viewModel.Summary);
            await _weatherForecastService.CreateAsync(forecast);

            // 2. Succès → Redirect
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            // 3. Validation → Rester sur la page
            _logger.LogWarning(ex, "Validation échouée lors de la création");
            ModelState.AddModelError("", ex.Message);
            
            await _publisher.PublishDomainExceptionAsync(User, ex);
            
            return View(viewModel);
        }
        catch (DomainException ex)
        {
            // 4. Autre erreur domain → Redirect avec notification
            _logger.LogError(ex, "Erreur domain lors de la création");
            TempData["ErrorMessage"] = ex.Message;
            
            await _publisher.PublishDomainExceptionAsync(User, ex);
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // 5. Erreur inattendue → Redirect avec notification générique
            _logger.LogError(ex, "Erreur inattendue lors de la création");
            var errorMessage = "Une erreur inattendue est survenue.";
            TempData["ErrorMessage"] = errorMessage;
            
            await _publisher.PublishGenericErrorAsync(
                User,
                errorMessage,
                "Create",
                "WeatherForecast",
                null,
                ex);
            
            return RedirectToAction(nameof(Index));
        }
    }

    return View(viewModel);
}
```

### Décisions Clés

| Type d'Exception | Action | Raison |
|------------------|--------|--------|
| `ValidationException` | `return View()` | User peut corriger |
| `EntityNotFoundException` | `RedirectToAction()` | Entité n'existe plus |
| `DatabaseException` | `RedirectToAction()` | Erreur temporaire |
| `Exception` (non gérée) | `RedirectToAction()` | Erreur inconnue |

---

## 📡 Notifications Temps Réel

### Event Publisher

```csharp
// application/Helpers/ErrorHelper.cs
public static class ErrorHelper
{
    public static async Task PublishDomainExceptionAsync(
        this IPublisher publisher,
        ClaimsPrincipal user,
        DomainException exception)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return;

        var errorEvent = new ErrorOccurredEvent(
            userId: userId,
            errorMessage: exception.Message,
            errorType: exception.ErrorType,
            action: exception.Action,
            entityType: exception.EntityType,
            entityId: exception.EntityId,
            stackTrace: exception.StackTrace
        );

        await publisher.Publish(errorEvent);
    }
}
```

### SignalRErrorHandler

```csharp
// application/Handlers/Error/SignalRErrorHandler.cs
public class SignalRErrorHandler : INotificationHandler<ErrorOccurredEvent>
{
    private readonly IHubContext<UsersHub> _usersHub;
    private readonly IPendingNotificationService _pending;
    private readonly ILogger<SignalRErrorHandler> _logger;

    public async Task Handle(ErrorOccurredEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                Message = notification.ErrorMessage,
                ErrorType = notification.ErrorType.ToString(),
                Action = notification.Action,
                EntityType = notification.EntityType,
                EntityId = notification.EntityId,
                OccurredAt = notification.OccurredAt,
                CorrelationId = notification.CorrelationId
            };

            // 1. Envoyer la notification SignalR
            await _usersHub.Clients.User(notification.UserId).SendAsync(
                "ErrorOccurred",
                payload,
                cancellationToken);

            _logger.LogInformation(
                "[SignalR] Notification d'erreur envoyée à {UserId} | CorrelationId={CorrelationId}",
                notification.UserId,
                notification.CorrelationId);

            // 2. Bufferiser dans Redis UNIQUEMENT pour les erreurs avec redirect
            // Les erreurs de validation ne sont PAS bufferisées (user reste sur la page)
            if (notification.ErrorType != ErrorType.Validation)
            {
                var payloadJson = JsonSerializer.Serialize(payload);
                await _pending.AddAsync(
                    "error",
                    notification.UserId,
                    "ErrorOccurred",
                    payloadJson,
                    TimeSpan.FromMinutes(2),
                    cancellationToken);

                _logger.LogDebug(
                    "[Redis] Notification bufferisée | CorrelationId={CorrelationId}",
                    notification.CorrelationId);
            }
            else
            {
                _logger.LogDebug(
                    "[Redis] Notification NON bufferisée (Validation) | CorrelationId={CorrelationId}",
                    notification.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la notification SignalR");
        }
    }
}
```

### Client JavaScript

```javascript
// application/wwwroot/js/hubs/user-realtime.js
usersConnection.on("ErrorOccurred", (payload) => {
    console.error("❌ Erreur reçue:", payload);
    
    const cId = payload?.CorrelationId || payload?.correlationId;
    
    // ✅ Déduplication
    if (hasProcessedCorrelation(cId)) {
        console.warn(`⚠️ Erreur déjà traitée (CorrelationId: ${cId})`);
        return;
    }
    
    const message = payload?.Message || payload?.message || "Une erreur est survenue";
    const errorType = payload?.ErrorType || payload?.errorType || "Unknown";
    const action = payload?.Action || payload?.action;
    const entityType = payload?.EntityType || payload?.entityType;
    
    // Construire un titre contextuel
    let title = "Erreur";
    if (action && entityType) {
        const actionText = getActionText(action);
        const entityText = getEntityText(entityType);
        title = `Erreur - ${actionText} ${entityText}`;
    }
    
    // Afficher la notification
    showNotification(title, message, "danger");
    
    // Marquer comme traité
    markProcessedCorrelation(cId);
    
    console.error(`[Error] CorrelationId=${cId} | Type=${errorType} | Message=${message}`);
});
```

---

## 💾 Bufferisation Redis

### Pourquoi Bufferiser ?

Quand un controller fait un **redirect**, la page se recharge complètement :
1. SignalR se déconnecte
2. La notification est envoyée pendant le redirect
3. Le client ne la reçoit pas
4. SignalR se reconnecte
5. Le client récupère les notifications en attente de Redis

### Stratégie Intelligente

```csharp
// ✅ Bufferiser UNIQUEMENT pour les erreurs avec redirect
if (notification.ErrorType != ErrorType.Validation)
{
    await _pending.AddAsync(...);
}
```

| Type d'Erreur | Redirect ? | Bufferiser ? |
|---------------|-----------|--------------|
| `Validation` | ❌ Non (`return View`) | ❌ Non |
| `Database` | ✅ Oui (`RedirectToAction`) | ✅ Oui |
| `NotFound` | ✅ Oui (`RedirectToAction`) | ✅ Oui |
| `External` | ✅ Oui (`RedirectToAction`) | ✅ Oui |

### Récupération au Reconnexion

```javascript
// application/wwwroot/js/hubs/user-realtime.js
usersConnection.onreconnected(async () => {
    console.log("✅ Reconnecté à SignalR");
    
    // Récupérer les notifications en attente
    await fetchAndDisplayPendingErrors();
});

async function fetchAndDisplayPendingErrors() {
    try {
        const response = await fetch('/api/notifications/pending');
        const notifications = await response.json();
        
        notifications.forEach(notification => {
            // Afficher chaque notification bufferisée
            showNotification(notification.title, notification.message, "danger");
        });
    } catch (error) {
        console.error("Erreur lors de la récupération des notifications:", error);
    }
}
```

---

## 🎨 AJAX et UX

### Problème avec POST Classique

```
User clique "Enregistrer"
   ↓
POST /WeatherForecast/Edit/3
   ↓
ValidationException levée
   ↓
return View(viewModel) → Page se recharge
   ↓
SignalR se déconnecte puis se reconnecte
   ↓
❌ Notification perdue pendant le refresh
```

### Solution : AJAX

```javascript
// application/Views/WeatherForecast/Edit.cshtml
document.getElementById('editForm').addEventListener('submit', async function(e) {
    e.preventDefault();  // ✅ Empêcher le submit classique
    
    const form = this;
    const formData = new FormData(form);
    const submitButton = form.querySelector('button[type="submit"]');
    
    // Désactiver le bouton
    submitButton.disabled = true;
    submitButton.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Enregistrement...';
    
    try {
        const response = await fetch(form.action, {
            method: 'POST',
            body: formData
        });
        
        if (response.redirected) {
            // ✅ Succès - Redirection
            window.location.href = response.url;
        } else {
            // ❌ Erreur - Rester sur la page
            const html = await response.text();
            
            // Parser le HTML pour extraire le message d'erreur
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const validationSummary = doc.querySelector('#validationSummary');
            
            if (validationSummary) {
                document.getElementById('validationSummary').innerHTML = validationSummary.innerHTML;
            }
            
            // ✅ La notification SignalR sera affichée automatiquement
            // car la connexion n'a pas été interrompue (pas de refresh)
        }
    } catch (error) {
        console.error('Erreur lors de la soumission:', error);
        showNotification('Erreur', 'Une erreur est survenue.', 'danger');
    } finally {
        // Réactiver le bouton
        submitButton.disabled = false;
        submitButton.innerHTML = 'Enregistrer';
    }
});
```

### Avantages

- ✅ **Pas de refresh** → SignalR reste connecté
- ✅ **Notification immédiate** → Affichée en temps réel
- ✅ **UX fluide** → Pas de rechargement
- ✅ **Formulaire conservé** → Données toujours là
- ✅ **Feedback visuel** → Spinner pendant l'enregistrement

---

## 🔄 Flux Complets

### Scénario 1 : Validation avec AJAX (Pas de Redirect)

```
1. User saisit "-- Sélectionnez --" et clique "Enregistrer"
   ↓
2. JavaScript: e.preventDefault() + AJAX POST
   ↓
3. WeatherForecast constructor: ValidateSummary()
   ↓
4. throw ValidationException("Veuillez sélectionner un résumé valide")
   ↓
5. Controller catch (ValidationException ex)
   ↓
6. ModelState.AddModelError() + PublishDomainExceptionAsync()
   ↓
7. return View(viewModel) → Réponse HTML (pas de redirect)
   ↓
8. SignalRErrorHandler:
   - SendAsync("ErrorOccurred", payload) ✅
   - PAS de bufferisation Redis (ErrorType.Validation) ✅
   ↓
9. Client JavaScript (connexion active):
   - Reçoit "ErrorOccurred"
   - Déduplication (CorrelationId)
   - showNotification("Erreur - Validation", "Veuillez sélectionner...", "danger") ✅
   ↓
10. AJAX parse la réponse HTML
   ↓
11. Affiche le message d'erreur dans le formulaire
   ↓
12. ✅ User voit :
    - Notification toast rouge en haut à droite
    - Message d'erreur dans le formulaire
    - Peut corriger et réessayer
```

---

### Scénario 2 : Erreur avec Redirect

```
1. User essaie de supprimer une prévision
   ↓
2. POST classique (pas AJAX)
   ↓
3. Service: throw DatabaseException("Erreur lors de la suppression")
   ↓
4. Controller catch (DomainException ex)
   ↓
5. TempData["ErrorMessage"] = ex.Message
   ↓
6. PublishDomainExceptionAsync()
   ↓
7. RedirectToAction(nameof(Index))
   ↓
8. SignalRErrorHandler:
   - SendAsync("ErrorOccurred", payload) ✅
   - Bufferisation Redis (ErrorType.Database) ✅
   ↓
9. Page se recharge (redirect)
   ↓
10. SignalR se déconnecte puis se reconnecte
   ↓
11. usersConnection.onreconnected()
   ↓
12. fetchAndDisplayPendingErrors()
   ↓
13. Récupère l'erreur bufferisée de Redis
   ↓
14. ✅ Notification affichée après le redirect
```

---

### Scénario 3 : Exception Non Gérée (Middleware)

```
1. Controller.SomeAction()
   ↓
2. var data = null;
   var result = data.ToString();  ❌ NullReferenceException
   ↓
3. Pas de catch dans le controller
   ↓
4. Exception remonte au GlobalErrorHandlerMiddleware
   ↓
5. catch (Exception ex)
   ↓
6. _logger.LogError(ex, "[GlobalErrorHandler] Exception non gérée")
   ↓
7. context.Response.Redirect("/Home/Error")
   ↓
8. ✅ User redirigé vers page d'erreur générique
   ↓
9. ✅ Erreur tracée dans les logs pour investigation
```

---

## ✅ Tests et Validation

### Test Manuel : Validation du Résumé

#### Étapes

1. Naviguer vers `/WeatherForecast/Create`
2. Remplir le formulaire :
   - Date : `2025-10-27`
   - Température : `25°C`
   - Résumé : **`-- Sélectionnez --`** ❌
3. Cliquer sur "Créer"

#### Résultat Attendu

- ✅ **Pas de redirect** : User reste sur `/WeatherForecast/Create`
- ✅ **Message d'erreur** : "Veuillez sélectionner un résumé météo valide."
- ✅ **Notification toast** : Rouge en haut à droite
- ✅ **Formulaire conservé** : Données toujours là
- ✅ **Log** : `[WARNING] Validation échouée lors de la création`

### Test Manuel : Révocation d'une Clé API Inexistante

#### Étapes

1. Naviguer vers `/ApiKeys/Revoke/999`
2. Cliquer sur "Révoquer"

#### Résultat Attendu

- ✅ **Redirect** : User redirigé vers `/ApiKeys`
- ✅ **Message d'erreur** : "ApiKey avec l'ID '999' est introuvable."
- ✅ **Notification toast** : Rouge après le redirect
- ✅ **Log** : `[ERROR] Erreur domain lors de la révocation`

### Checklist Complète

#### WeatherForecast

- [ ] Create avec résumé invalide → Notification + Message formulaire
- [ ] Edit avec résumé invalide → Notification + Message formulaire
- [ ] Delete avec ID invalide → Notification après redirect

#### ApiKey

- [ ] Create avec nom vide → Notification + Message formulaire
- [ ] Revoke d'une clé inexistante → Notification après redirect
- [ ] Revoke d'une clé d'un autre user → Notification après redirect

#### Middleware

- [ ] Exception non gérée → Redirect vers `/Home/Error` + Log

---

## 📊 Couverture Complète

### Entités Couvertes

| Entité | Create | Update | Delete | Revoke |
|--------|--------|--------|--------|--------|
| **WeatherForecast** | ✅ | ✅ | ✅ | - |
| **ApiKey** | ✅ | - | - | ✅ |

### Types d'Erreurs

| Type | Description | Exemple |
|------|-------------|---------|
| `Validation` | Données invalides | Résumé vide, nom vide |
| `NotFound` | Entité introuvable | ID inexistant |
| `Database` | Erreur DB | Contrainte violée, timeout |
| `External` | Service externe | Email, Redis |
| `Authorization` | Pas autorisé | Pas propriétaire |
| `Unknown` | Erreur inconnue | Bug, NullRef |

---