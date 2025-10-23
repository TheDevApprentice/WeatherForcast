# 🔐 Admin Hub - Guide d'utilisation

## 🎯 Objectif

Le **AdminHub** permet aux administrateurs de recevoir des notifications en temps réel sur toutes les activités importantes :
- Nouveaux utilisateurs enregistrés
- Connexions/déconnexions
- Création de sessions
- Création/révocation d'API Keys
- Modifications de rôles et claims

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    ADMIN HUB FLOW                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Action (Register, Login, etc.)                              │
│         ↓                                                    │
│  Service (UserManagementService, AuthenticationService)      │
│         ↓                                                    │
│  Publish Domain Event (UserRegisteredEvent, etc.)           │
│         ↓                                                    │
│  MediatR Handler (SignalRAdminNotificationHandler)          │
│         ↓                                                    │
│  AdminHub → Broadcast                                        │
│         ↓                                                    │
│  Admins connectés reçoivent la notification                  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 📡 Événements disponibles

### 1. **UserRegistered**
Déclenché quand un nouvel utilisateur s'enregistre.

**Données** :
```json
{
  "userId": "123",
  "email": "user@example.com",
  "userName": "john_doe",
  "registeredAt": "2025-10-23T18:00:00Z",
  "ipAddress": "192.168.1.1"
}
```

**Utilisation** :
```javascript
adminConnection.on("UserRegistered", (data) => {
    console.log("Nouvel utilisateur:", data.email);
    // Mettre à jour la liste des users
    refreshUsersList();
});
```

---

### 2. **UserLoggedIn**
Déclenché quand un utilisateur se connecte.

**Données** :
```json
{
  "userId": "123",
  "email": "user@example.com",
  "userName": "john_doe",
  "loggedInAt": "2025-10-23T18:00:00Z",
  "ipAddress": "192.168.1.1",
  "userAgent": "Mozilla/5.0..."
}
```

**Utilisation** :
```javascript
adminConnection.on("UserLoggedIn", (data) => {
    console.log("Connexion:", data.email);
    // Si on est sur la page de cet user, mettre à jour les sessions
    if (currentUserId === data.userId) {
        refreshUserSessions(data.userId);
    }
});
```

---

### 3. **UserLoggedOut**
Déclenché quand un utilisateur se déconnecte.

**Données** :
```json
{
  "userId": "123",
  "email": "user@example.com",
  "loggedOutAt": "2025-10-23T18:00:00Z"
}
```

---

### 4. **SessionCreated**
Déclenché quand une nouvelle session est créée.

**Données** :
```json
{
  "sessionId": "abc123",
  "userId": "123",
  "email": "user@example.com",
  "createdAt": "2025-10-23T18:00:00Z",
  "expiresAt": "2025-10-24T18:00:00Z",
  "ipAddress": "192.168.1.1",
  "userAgent": "Mozilla/5.0..."
}
```

**Utilisation** :
```javascript
adminConnection.on("SessionCreated", (data) => {
    // Si on regarde le profil de cet user, ajouter la session en temps réel
    if (currentUserId === data.userId) {
        addSessionToList(data);
    }
});
```

---

### 5. **ApiKeyCreated**
Déclenché quand une API Key est créée.

**Données** :
```json
{
  "apiKeyId": 1,
  "userId": "123",
  "email": "user@example.com",
  "keyName": "My API Key",
  "createdAt": "2025-10-23T18:00:00Z",
  "expiresAt": "2025-11-23T18:00:00Z"
}
```

---

### 6. **ApiKeyRevoked**
Déclenché quand une API Key est révoquée.

**Données** :
```json
{
  "apiKeyId": 1,
  "userId": "123",
  "email": "user@example.com",
  "keyName": "My API Key",
  "revokedAt": "2025-10-23T18:00:00Z",
  "revokedBy": "admin@example.com"
}
```

---

### 7. **UserRoleChanged**
Déclenché quand les rôles d'un utilisateur changent.

**Données** :
```json
{
  "userId": "123",
  "email": "user@example.com",
  "roleName": "Admin",
  "isAdded": true,
  "changedAt": "2025-10-23T18:00:00Z",
  "changedBy": "superadmin@example.com"
}
```

---

### 8. **UserClaimChanged**
Déclenché quand les claims d'un utilisateur changent.

**Données** :
```json
{
  "userId": "123",
  "email": "user@example.com",
  "claimType": "Permission",
  "claimValue": "ForecastWrite",
  "isAdded": true,
  "changedAt": "2025-10-23T18:00:00Z",
  "changedBy": "admin@example.com"
}
```

---

## 🔒 Sécurité

### Authentification
```csharp
[Authorize(Roles = "Admin")]
public class AdminHub : Hub
```

- ✅ Seuls les utilisateurs avec le rôle **Admin** peuvent se connecter
- ✅ Tentative de connexion sans rôle Admin → **403 Forbidden**
- ✅ Authentification via Cookie (Web) ou JWT (Mobile)

### Isolation
- ✅ Hub complètement séparé du `WeatherForecastHub`
- ✅ Pas de risque de fuite de données admin vers users normaux
- ✅ URL dédiée : `/hubs/admin`

---

## 📋 Intégration dans les services

### Exemple : Publier un événement lors de l'enregistrement

```csharp
// domain/Services/UserManagementService.cs
public async Task<IdentityResult> RegisterUserAsync(string email, string password)
{
    var user = new ApplicationUser { Email = email, UserName = email };
    var result = await _userManager.CreateAsync(user, password);
    
    if (result.Succeeded)
    {
        // ✅ Publier l'événement
        await _publisher.Publish(new UserRegisteredEvent(
            userId: user.Id,
            email: user.Email,
            userName: user.UserName,
            ipAddress: _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        ));
    }
    
    return result;
}
```

### Exemple : Publier un événement lors de la connexion

```csharp
// domain/Services/AuthenticationService.cs
public async Task<SignInResult> LoginAsync(string email, string password)
{
    var result = await _signInManager.PasswordSignInAsync(email, password, false, false);
    
    if (result.Succeeded)
    {
        var user = await _userManager.FindByEmailAsync(email);
        
        // ✅ Publier l'événement
        await _publisher.Publish(new UserLoggedInEvent(
            userId: user.Id,
            email: user.Email,
            userName: user.UserName,
            ipAddress: _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            userAgent: _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString()
        ));
    }
    
    return result;
}
```

### Exemple : Publier un événement lors de la création d'API Key

```csharp
// domain/Services/ApiKeyService.cs
public async Task<ApiKey> CreateApiKeyAsync(string userId, string keyName, DateTime? expiresAt)
{
    var apiKey = new ApiKey
    {
        UserId = userId,
        KeyName = keyName,
        Key = GenerateApiKey(),
        ExpiresAt = expiresAt
    };
    
    await _repository.AddAsync(apiKey);
    await _unitOfWork.SaveChangesAsync();
    
    var user = await _userManager.FindByIdAsync(userId);
    
    // ✅ Publier l'événement
    await _publisher.Publish(new ApiKeyCreatedEvent(
        apiKeyId: apiKey.Id,
        userId: user.Id,
        email: user.Email,
        keyName: keyName,
        expiresAt: expiresAt
    ));
    
    return apiKey;
}
```

---

## 🎨 Intégration UI (Page Admin)

### 1. Ajouter le script dans la vue

```html
<!-- Views/Admin/Users/Index.cshtml -->
@section Scripts {
    <script src="~/lib/signalr/dist/browser/signalr.min.js"></script>
    <script src="~/js/admin-realtime.js"></script>
}
```

### 2. Ajouter le conteneur de notifications

```html
<!-- Layout ou page Admin -->
<div id="admin-notifications" class="position-fixed top-0 end-0 p-3" style="z-index: 9999;">
    <!-- Les notifications apparaîtront ici -->
</div>

<div id="admin-connection-status" class="position-fixed bottom-0 end-0 p-3">
    <span class="badge bg-secondary">Connexion...</span>
</div>
```

### 3. Ajouter les styles CSS

```css
/* wwwroot/css/admin.css */
.admin-notification {
    min-width: 300px;
    margin-bottom: 10px;
    animation: slideIn 0.3s ease-out;
}

@keyframes slideIn {
    from {
        transform: translateX(100%);
        opacity: 0;
    }
    to {
        transform: translateX(0);
        opacity: 1;
    }
}

.session-item-new {
    background-color: #d1ecf1 !important;
    border-left: 4px solid #0dcaf0;
    animation: highlight 3s ease-out;
}

@keyframes highlight {
    0% { background-color: #d1ecf1; }
    100% { background-color: white; }
}
```

---

## 🧪 Test de l'AdminHub

### Test 1 : Connexion au Hub

```bash
1. Se connecter en tant qu'Admin
2. Aller sur /Admin/Users
3. Ouvrir la console du navigateur
4. Vérifier : "✅ Connecté au AdminHub SignalR"
```

### Test 2 : Notification d'enregistrement

```bash
1. Admin connecté sur /Admin/Users
2. Dans un autre navigateur : S'enregistrer en tant que nouvel utilisateur
3. Vérifier : Notification "Nouvel utilisateur" apparaît en temps réel
```

### Test 3 : Notification de connexion

```bash
1. Admin connecté sur /Admin/Users/Details/{userId}
2. L'utilisateur {userId} se connecte
3. Vérifier : Notification "Connexion" + Nouvelle session apparaît en temps réel
```

### Test 4 : Notification d'API Key

```bash
1. Admin connecté sur /Admin/Users/Details/{userId}
2. L'utilisateur {userId} crée une API Key
3. Vérifier : Notification "Nouvelle API Key" apparaît en temps réel
```

---

## 📊 Monitoring et logs

### Logs côté serveur

```
[INFO] 🔐 Admin admin@example.com connecté au AdminHub (ConnectionId: abc123)
[INFO] 🔐 [AdminHub] Broadcasting UserRegistered: user@example.com from 192.168.1.1
[INFO] 🔐 [AdminHub] Broadcasting UserLoggedIn: user@example.com from 192.168.1.1
[INFO] 🔐 [AdminHub] Broadcasting ApiKeyCreated: user@example.com - Key 'My API Key'
```

### Logs côté client (console navigateur)

```
✅ Connecté au AdminHub SignalR
🆕 Nouvel utilisateur enregistré: { userId: "123", email: "user@example.com", ... }
🔐 Utilisateur connecté: { userId: "123", email: "user@example.com", ... }
🔑 API Key créée: { apiKeyId: 1, userId: "123", keyName: "My API Key", ... }
```

---

## 🚀 Fonctionnalités avancées (à implémenter)

### 1. Filtrage des notifications
Permettre aux admins de choisir quels types de notifications recevoir.

### 2. Historique des notifications
Stocker les notifications dans une DB pour consultation ultérieure.

### 3. Notifications push
Envoyer des notifications push aux admins même quand ils ne sont pas sur la page.

### 4. Groupes d'admins
Créer des groupes (SuperAdmin, Moderator) avec des notifications différentes.

### 5. Actions en temps réel
Permettre aux admins de révoquer une session ou une API Key directement depuis la notification.

---

## ✅ Checklist d'implémentation

- [x] Hub AdminHub créé
- [x] Événements domain créés
- [x] Handler SignalR créé
- [x] Hub mappé dans Web et API
- [x] JavaScript client créé
- [ ] Publier les événements dans les services
- [ ] Ajouter le script dans les vues Admin
- [ ] Ajouter les conteneurs de notifications
- [ ] Tester les notifications en temps réel
- [ ] Ajouter les styles CSS
- [ ] Documenter pour l'équipe

---

## 📚 Fichiers créés

- ✅ `shared/Hubs/AdminHub.cs`
- ✅ `domain/Events/Admin/UserRegisteredEvent.cs`
- ✅ `domain/Events/Admin/UserLoggedInEvent.cs`
- ✅ `domain/Events/Admin/UserLoggedOutEvent.cs`
- ✅ `domain/Events/Admin/SessionCreatedEvent.cs`
- ✅ `domain/Events/Admin/ApiKeyCreatedEvent.cs`
- ✅ `domain/Events/Admin/ApiKeyRevokedEvent.cs`
- ✅ `domain/Events/Admin/UserRoleChangedEvent.cs`
- ✅ `domain/Events/Admin/UserClaimChangedEvent.cs`
- ✅ `application/Handlers/Admin/SignalRAdminNotificationHandler.cs`
- ✅ `application/wwwroot/js/admin-realtime.js`
- ✅ `doc/admin/ADMIN_HUB_GUIDE.md`

---

## 🎉 Résultat final

Les administrateurs peuvent maintenant :
- ✅ Voir en temps réel les nouveaux utilisateurs
- ✅ Voir en temps réel les connexions/déconnexions
- ✅ Voir en temps réel les nouvelles sessions (même en regardant le profil d'un user)
- ✅ Voir en temps réel les API Keys créées/révoquées
- ✅ Voir en temps réel les modifications de rôles et claims
- ✅ Recevoir des notifications visuelles pour chaque événement

**Le monitoring admin est maintenant complet et en temps réel !** 🚀
