# 🌤️ WeatherForecast - Application ASP.NET Core 8.0

Application complète démontrant une **Clean Architecture** avec communication temps réel, authentification multi-niveaux et architecture événementielle.

## 📋 Table des Matières

- [Vue d'Ensemble](#-vue-densemble)
- [Architecture](#-architecture)
- [Technologies](#-technologies)
- [Fonctionnalités](#-fonctionnalités)
- [Démarrage Rapide](#-démarrage-rapide)
- [Structure du Projet](#-structure-du-projet)
- [Patterns Implémentés](#-patterns-implémentés)
- [Documentation Détaillée](#-documentation-détaillée)
- [Tests](#-tests)

---

## 🎯 Vue d'Ensemble

Cette application démontre une architecture de production complète pour des applications ASP.NET Core modernes avec :

- **Clean Architecture** avec séparation stricte des responsabilités (Domain, Application, Infrastructure, API)
- **Domain Events** avec Event Bus custom (remplace MediatR)
- **Communication temps réel** via SignalR et Redis Pub/Sub (11 canaux)
- **Authentification multi-niveaux** (JWT pour API, API Keys OAuth2, Identity pour Web)
- **Notifications temps réel** entre clients avec exclusion émetteur
- **Sécurité robuste** (Argon2id, Rate Limiting, Session Validation)
- **Performance optimisée** (DbContext Pooling, Index composites, Redis Cache)

### 🎓 Cas d'Usage

Cette application est idéale pour :
- **Apprendre** les patterns modernes ASP.NET Core et DDD
- **Démarrer** un nouveau projet avec une architecture solide
- **Comprendre** l'architecture événementielle et CQRS
- **Implémenter** des notifications temps réel robustes
- **Étudier** une séparation API/Web App fonctionnelle
- **Découvrir** les bonnes pratiques de sécurité (OWASP 2024)

---

## 🏗️ Architecture

### Diagramme de l'Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    CLIENTS WEB (Browsers)                    │
│                  SignalR WebSocket Connection                │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  APPLICATION WEB (MVC)                       │
│  • Authentification Identity                                │
│  • Gestion des prévisions (CRUD)                            │
│  • SignalR Hub (notifications temps réel)                   │
│  • Redis Subscriber (écoute les events)                     │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    REDIS PUB/SUB                             │
│  • Canal: weatherforecast.created                           │
│  • Canal: weatherforecast.updated                           │
│  • Canal: weatherforecast.deleted                           │
└───────────────────────────┬─────────────────────────────────┘
                            ▲
                            │
┌───────────────────────────┴─────────────────────────────────┐
│                      API REST                                │
│  • Authentification JWT + API Keys                          │
│  • Endpoints publics (lecture seule)                        │
│  • Rate Limiting (100 req/min)                              │
│  • Redis Publisher (publie les events)                      │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   DOMAIN LAYER                               │
│  • Entities (WeatherForecast, User, Session, etc.)         │
│  • Domain Events (ForecastCreated, Updated, Deleted)        │
│  • Services (Business Logic)                                │
│  • Interfaces (Repositories, Services)                      │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                INFRASTRUCTURE LAYER                          │
│  • Repositories (EF Core)                                   │
│  • Unit of Work                                             │
│  • PostgreSQL Database                                      │
│  • Data Migrations                                          │
└─────────────────────────────────────────────────────────────┘
```

### Flux de Communication Temps Réel

#### Scénario : Création d'une Prévision Météo

```
User (Browser 1) → Web App → WeatherForecastService → EventPublisher
                                                            ↓
                                                  Event Handlers (parallèles):
                                                  1. AuditLogHandler (logs)
                                                  2. SignalRHandler (broadcast direct)
                                                  3. RedisBrokerHandler → Redis Pub
                                                                            ↓
                                                                      Redis Channel
                                                                      (weatherforecast.created)
                                                                            ↓
                                                          RedisSubscriberService (Background)
                                                                            ↓
                                                          SignalR Hub → Tous les Clients
                                                                            ↓
                                          User (Browser 2) ✅ Notification temps réel
```

**Points clés** :
- ✅ **Handlers parallèles** : Exécution simultanée avec Task.WhenAll
- ✅ **Exclusion émetteur** : SignalRConnectionService évite les boucles
- ✅ **Communication inter-processus** : API et Web App communiquent via Redis
- ✅ **Reconnexion automatique** : Client JavaScript avec retry exponentiel

---

## 🛠️ Technologies

### Backend
- **ASP.NET Core 8.0** - Framework web moderne
- **Entity Framework Core 8.0** - ORM avec DbContext Pooling (256 instances)
- **PostgreSQL 16** - Base de données relationnelle avec index composites
- **Redis 7** - Pub/Sub (11 canaux), Cache distribué, Connection Mapping
- **SignalR** - Communication WebSocket temps réel avec reconnexion automatique
- **StackExchange.Redis** - Client Redis haute performance
- **Argon2id** - Hashing sécurisé (64MB RAM, 4 iterations) - OWASP 2024

### Frontend
- **Razor Pages / MVC** - Interface web avec ViewModels
- **Bootstrap 5** - UI Framework responsive
- **JavaScript ES6+** - Modules natifs
- **Lucide Icons** - Icônes modernes
- **SignalR JavaScript Client** - Notifications temps réel avec retry

### Authentification & Sécurité
- **ASP.NET Core Identity** - Gestion utilisateurs avec sessions (Web)
- **JWT Bearer (RS256)** - Authentification API avec validation en base
- **API Keys (OAuth2)** - Client Credentials avec Argon2id
- **Rate Limiting Redis** - 100 req/min avec brute force protection (5 tentatives, 15min blocage)
- **Session Validation** - Vérification DB à chaque requête
- **CSP, HSTS** - Headers de sécurité complets
- **Data Protection** - Clés chiffrées avec certificat X.509

### Tests
- **NUnit 4.0** - Framework de tests unitaires
- **FluentAssertions** - Assertions expressives
- **Moq** - Mocking pour tests

### DevOps
- **Docker** - Containerisation multi-stage
- **Docker Compose** - Orchestration locale et production
- **GitHub Actions** - CI/CD automatisé
- **Azure Pipelines** - Déploiement continu

---

## ✨ Fonctionnalités

### 🔐 Authentification Multi-Niveaux

#### Application Web (MVC)
- ✅ **ASP.NET Core Identity** : Inscription/Connexion avec validation
- ✅ **Sessions sécurisées** : Gestion en base avec révocation temps réel
- ✅ **Cookies HttpOnly** : Protection XSS
- ✅ **Audit complet** : Traçabilité de toutes les connexions
- ✅ **Logout forcé** : Via SessionRevokedEvent + SignalR

#### API REST
- ✅ **JWT Bearer (RS256)** : Tokens signés avec validation en base
- ✅ **API Keys OAuth2** : Client Credentials avec Argon2id (64MB RAM)
- ✅ **Rate Limiting** : 100 req/min par IP avec Redis
- ✅ **Brute Force Protection** : 5 tentatives max, blocage 15 minutes
- ✅ **Swagger OAuth2** : Documentation interactive avec authentification

### 📡 Notifications Temps Réel

- ✅ **3 SignalR Hubs** : WeatherForecastHub, AdminHub, UsersHub
- ✅ **Redis Pub/Sub** : 11 canaux pour communication inter-processus
- ✅ **Notifications automatiques** pour :
  - **Prévisions** : Création, modification, suppression
  - **Admin** : Nouveaux utilisateurs, sessions, API Keys
  - **Utilisateurs** : Emails reçus, logout forcé
- ✅ **Exclusion émetteur** : Évite les boucles de notification
- ✅ **Reconnexion automatique** : Retry exponentiel côté client
- ✅ **Connection Mapping Redis** : Notifications ciblées par utilisateur

### 🎯 Architecture Événementielle

```csharp
// Création d'une prévision
await _service.CreateAsync(forecast);
    ↓
// EventPublisher publie automatiquement
await _publisher.Publish(new ForecastCreatedEvent(forecast));
    ↓
// Handlers s'exécutent en PARALLÈLE (Task.WhenAll)
1. AuditLogHandler → Logs console avec métriques
2. SignalRHandler → Broadcast direct aux clients connectés
3. RedisBrokerHandler → Publie vers Redis Pub/Sub
    ↓
// RedisSubscriberService (BackgroundService)
Écoute 11 canaux Redis → Broadcaste vers SignalR Hubs
```

**Événements implémentés (19)** :
- **WeatherForecast** : Created, Updated, Deleted
- **Admin** : UserRegistered, UserLoggedIn, SessionCreated, ApiKeyCreated, etc.
- **Mailing** : EmailSentToUser, VerificationEmailSentToUser

### 🔒 Sécurité de Production

- ✅ **Cryptographie moderne** : Argon2id recommandé OWASP 2024
- ✅ **Constant-time comparison** : Protection timing attacks
- ✅ **Headers sécurité** : CSP avec nonce, X-Frame-Options: DENY, HSTS
- ✅ **HTTPS forcé** : Redirection automatique
- ✅ **Data Protection** : Clés chiffrées avec certificat X.509
- ✅ **CORS configuré** : Origines autorisées uniquement
- ✅ **Anti-forgery tokens** : Sur tous les formulaires POST
- ✅ **Session Validation** : Vérification DB à chaque requête
- ✅ **Redis authentifié** : Mot de passe fort requis

---

## 🚀 Démarrage Rapide

### Prérequis

- ✅ [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) ou supérieur
- ✅ [Docker Desktop](https://www.docker.com/products/docker-desktop) pour PostgreSQL et Redis
- ✅ [Visual Studio 2022](https://visualstudio.microsoft.com/) ou [VS Code](https://code.visualstudio.com/)
- ✅ [Git](https://git-scm.com/) pour cloner le repository

### Installation (5 minutes)

#### 1️⃣ Cloner le Repository

```bash
git clone <votre-repo>
cd test
```

#### 2️⃣ Démarrer PostgreSQL et Redis

**Windows (PowerShell)** :
```powershell
.\scripts\setup-database.ps1
```

**Linux/macOS (Bash)** :
```bash
chmod +x ./scripts/setup-database.sh
./scripts/setup-database.sh
```

Ce script va :
- ✅ Vérifier Docker
- ✅ Démarrer PostgreSQL (port 5432)
- ✅ Démarrer Redis (port 6379)
- ✅ Créer les migrations EF Core
- ✅ Initialiser la base de données

**Résultat attendu** :
```
========================================
  SETUP TERMINE AVEC SUCCES
========================================

[INFO] Base de donnees PostgreSQL prete:
   Host: localhost:5432
   Database: weatherforecastdb
   User: weatheruser

[INFO] Redis pret:
   Host: localhost:6379
   Password: redisSecurePass123!
```

#### 3️⃣ Configurer Visual Studio pour Démarrer les 2 Projets

1. **Clic droit sur la solution** → **Propriétés**
2. **Projets de démarrage** → Sélectionner **"Plusieurs projets de démarrage"**
3. Configurer :
   - **`api`** → Action : **Démarrer**
   - **`application`** → Action : **Démarrer**
4. **Appliquer** → **OK**

![Configuration Visual Studio](docs/images/visual-studio-multiple-startup.png)

#### 4️⃣ Lancer l'Application

**Dans Visual Studio** :
- Appuyer sur **F5** (ou cliquer sur le bouton ▶️ Docker)

**Ou en ligne de commande** :

Terminal 1 (API) :
```bash
cd api
dotnet run
```

Terminal 2 (Web App) :
```bash
cd application
dotnet run
```

#### 5️⃣ Accéder aux Applications

| Application | URL | Description |
|------------|-----|-------------|
| **Web App** | https://localhost:7203 | Interface utilisateur principale |
| **API** | https://localhost:7252 | API REST publique |
| **Swagger** | https://localhost:7252/swagger/index.html | Documentation API interactive |

### 🎉 Premier Test - Notifications Temps Réel

1. **Ouvrir 2 navigateurs** (ou 2 onglets en navigation privée) sur https://localhost:7203
2. **Créer un compte** sur chaque navigateur
3. **Navigateur 1** : Créer/Modifier/Supprimer une prévision météo
4. **Navigateur 2** : 🎊 **La modification apparaît instantanément en temps réel !**

**Ce qui se passe en coulisses** :
```
Navigateur 1 → Web App → Service → EventPublisher
                                        ↓
                            3 Handlers parallèles :
                            - SignalRHandler (broadcast direct)
                            - RedisBrokerHandler (Redis Pub/Sub)
                            - AuditLogHandler (logs)
                                        ↓
                            RedisSubscriberService
                                        ↓
                            SignalR Hub → Navigateur 2 ✅
```

---

## 📁 Structure du Projet

```
WeatherForecast/
├── 📂 domain/                      # Couche Domain (Cœur métier)
│   ├── Entities/                   # 5 Entités riches avec encapsulation
│   │   ├── WeatherForecast.cs      # Prévision avec Value Object Temperature
│   │   ├── ApplicationUser.cs      # Utilisateur (hérite IdentityUser)
│   │   ├── Session.cs              # Session Web/API avec révocation
│   │   ├── ApiKey.cs               # Clé API OAuth2 avec scopes
│   │   └── UserSession.cs          # Table liaison Many-to-Many
│   ├── ValueObjects/               # 2 Value Objects immutables
│   │   ├── Temperature.cs          # Température avec validation
│   │   └── ApiKeyScopes.cs         # Scopes OAuth2
│   ├── Events/                     # 19 Domain Events
│   │   ├── WeatherForecast/        # ForecastCreated, Updated, Deleted
│   │   ├── Admin/                  # UserRegistered, SessionCreated, etc.
│   │   └── Mailing/                # EmailSent, VerificationEmailSent
│   ├── Interfaces/                 # 13 Interfaces (Repositories, Services)
│   │   ├── IUnitOfWork.cs
│   │   ├── Repositories/           # 4 repositories
│   │   └── Services/               # 9 services métier
│   ├── Services/                   # 11 Services métier découplés
│   │   ├── WeatherForecastService.cs
│   │   ├── UserManagementService.cs
│   │   ├── AuthenticationService.cs
│   │   ├── SessionManagementService.cs
│   │   ├── ApiKeyService.cs        # Argon2id hashing
│   │   └── RateLimitService.cs     # Rate limiting Redis
│   └── Constants/                  # AppRoles, AppClaims, EmailOptions
│
├── 📂 infra/                       # Couche Infrastructure
│   ├── DbContext/
│   │   ├── AppDbContext.cs         # EF Core avec Owned Entities
│   │   ├── UnitOfWork.cs           # Pattern avec lazy loading
│   │   └── RoleSeeder.cs           # Seed rôles et claims
│   ├── Repositories/               # 4 Repositories
│   │   ├── WeatherForecastRepository.cs
│   │   ├── UserRepository.cs       # Recherche paginée
│   │   ├── SessionRepository.cs
│   │   └── ApiKeyRepository.cs
│   └── Data/Migrations/            # Migrations EF Core
│
├── 📂 application/                 # Application Web (MVC)
│   ├── Controllers/                # 6 Contrôleurs MVC
│   │   ├── HomeController.cs
│   │   ├── AuthController.cs       # Login/Register/Logout
│   │   ├── WeatherForecastController.cs
│   │   ├── ApiKeysController.cs    # Gestion clés utilisateur
│   │   ├── AdminController.cs      # Dashboard admin
│   │   └── AdminApiKeysController.cs
│   ├── BackgroundServices/
│   │   └── RedisSubscriberService.cs  # Écoute 11 canaux Redis
│   ├── Handlers/                   # 7 Event Handlers
│   │   ├── SignalRForecastNotificationHandler.cs
│   │   ├── SignalRAdminNotificationHandler.cs
│   │   └── SignalRUsersSessionNotificationHandler.cs
│   ├── Middleware/                 # 2 Middleware custom
│   │   ├── SessionValidationMiddleware.cs
│   │   └── RateLimitMiddleware.cs
│   ├── Authorization/              # Authorization custom
│   │   ├── PermissionHandler.cs
│   │   └── HasPermissionAttribute.cs
│   ├── Views/                      # Vues Razor
│   ├── wwwroot/js/                 # JavaScript ES6+ modules
│   └── Program.cs                  # Configuration complète
│
├── 📂 api/                         # API REST Publique
│   ├── Controllers/                # 2 Contrôleurs API
│   │   ├── AuthController.cs       # JWT Login/Register
│   │   └── WeatherForecastController.cs
│   ├── Handlers/                   # 5 Event Handlers
│   │   ├── RedisBrokerHandler.cs   # Publie vers Redis
│   │   └── ApiAuditLogHandler.cs
│   ├── Middleware/                 # 3 Middleware
│   │   ├── ApiKeyAuthenticationMiddleware.cs
│   │   ├── JwtSessionValidationMiddleware.cs
│   │   └── RateLimitMiddleware.cs
│   └── Program.cs                  # Configuration API
│
├── 📂 shared/                      # Composants partagés
│   ├── Hubs/                       # 3 SignalR Hubs
│   │   ├── WeatherForecastHub.cs
│   │   ├── AdminHub.cs
│   │   └── UsersHub.cs
│   └── Messaging/                  # Event Bus custom
│       ├── EventPublisher.cs       # Remplace MediatR
│       └── ServiceCollectionExtensions.cs
│
├── 📂 tests/                       # 18 Fichiers de tests
│   ├── Domain/                     # Tests entités, services, ValueObjects
│   ├── Infra/                      # Tests repositories
│   └── Api/                        # Tests middleware
│
├── 📂 scripts/                     # Scripts utilitaires
│   ├── setup-database.ps1          # Setup Windows
│   ├── setup-database.sh           # Setup Linux/macOS
│   └── apply-migrations.ps1        # Appliquer migrations
│
├── 📂 doc/                         # Documentation complète
│   ├── architecture/               # DOMAIN_EVENTS.md, REDIS_PUBSUB.md
│   ├── code/                       # Readme.md (analyse technique)
│   └── production/                 # CHECKLIST-PRODUCTION.md
│
├── docker-compose.yml              # Production
├── .env.production                 # Variables production
├── .env                            # Variables développement
└── README.md                       # Ce fichier
```

---

## 🎨 Patterns et Principes Implémentés

### 1. Clean Architecture + DDD

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│    (API Controllers, MVC Views)         │
└───────────────┬─────────────────────────┘
                │ Dépend de ↓
┌───────────────▼─────────────────────────┐
│        Application Layer                │
│  (Use Cases, Event Handlers)            │
└───────────────┬─────────────────────────┘
                │ Dépend de ↓
┌───────────────▼─────────────────────────┐
│          Domain Layer                   │
│  (Entities, Services, Events)           │  ← Cœur métier (AUCUNE dépendance)
│  (Value Objects, Interfaces)            │
└───────────────┬─────────────────────────┘
                ↑ Implémente
┌───────────────┴─────────────────────────┐
│      Infrastructure Layer               │
│  (EF Core, Repositories, Redis)         │
└─────────────────────────────────────────┘
```

**Avantages** :
- ✅ **Indépendance du framework** : Le domaine ne connaît pas ASP.NET Core
- ✅ **Testabilité maximale** : Toutes les dépendances mockables
- ✅ **SOLID** : Respect rigoureux des 5 principes
- ✅ **Évolutivité** : Ajout de fonctionnalités sans casser l'existant

### 2. Repository Pattern + Unit of Work

```csharp
// Pattern avec lazy loading et transactions
public class WeatherForecastService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<bool> UpdateAsync(int id, DateTime date, Temperature temperature, string? summary)
    {
        var forecast = await _unitOfWork.WeatherForecasts.GetByIdAsync(id);
        if (forecast == null) return false;
        
        // Méthodes métier de l'entité
        forecast.UpdateDate(date);
        forecast.UpdateTemperature(temperature);
        forecast.UpdateSummary(summary);
        
        await _unitOfWork.SaveChangesAsync(); // Transaction unique
        return true;
    }
}
```

**Avantages** :
- ✅ **Abstraction complète** : Le domaine ne connaît pas EF Core
- ✅ **Transactions cohérentes** : SaveChanges unique
- ✅ **Lazy loading** : Repositories instanciés à la demande
- ✅ **Tests faciles** : Mocking des repositories

### 3. Domain Events (Architecture Événementielle)

```csharp
// Event Bus custom (remplace MediatR)
await _publisher.Publish(new ForecastCreatedEvent(forecast));
    ↓
// Handlers s'exécutent en PARALLÈLE (Task.WhenAll)
public class AuditLogHandler : INotificationHandler<ForecastCreatedEvent>
public class SignalRHandler : INotificationHandler<ForecastCreatedEvent>
public class RedisBrokerHandler : INotificationHandler<ForecastCreatedEvent>
```

**Avantages** :
- ✅ **Découplage total** : Les handlers ne se connaissent pas
- ✅ **Extensibilité** : Nouveau handler = nouvelle classe (OCP)
- ✅ **Performance** : Exécution parallèle avec métriques
- ✅ **Traçabilité** : Corrélation ID pour suivre les événements

### 4. CQRS Léger

Séparation lecture/écriture :

```csharp
// Lecture (AsNoTracking pour performance)
public async Task<IEnumerable<WeatherForecast>> GetAllAsync()
{
    return await _context.WeatherForecasts
        .AsNoTracking() // Pas de tracking EF Core
        .ToListAsync();
}

// Écriture (avec tracking pour détection changements)
public async Task<bool> UpdateAsync(WeatherForecast forecast)
{
    _context.WeatherForecasts.Update(forecast);
    return await _context.SaveChangesAsync() > 0;
}
```

### 5. Pub/Sub Pattern (Redis)

Communication inter-processus asynchrone :

```
API (Publisher) → Redis Pub/Sub (11 canaux) → RedisSubscriberService → SignalR → Clients
```

**Avantages** :
- ✅ **Scalabilité** : API et Web App déployables séparément
- ✅ **Résilience** : Si Redis tombe, les apps continuent
- ✅ **Temps réel** : Notifications instantanées

### 6. Value Objects (DDD)

```csharp
// Temperature : Value Object immutable
public class Temperature
{
    public int Celsius { get; }
    public int Fahrenheit => 32 + (int)(Celsius / 0.5556);
    public bool IsHot => Celsius > 25;
    public bool IsCold => Celsius < 10;
    
    public Temperature(int celsius)
    {
        if (celsius < -100 || celsius > 100)
            throw new ArgumentException("Température invalide");
        Celsius = celsius;
    }
}
```

**Avantages** :
- ✅ **Immutabilité** : Pas de setters
- ✅ **Validation** : Dans le constructeur
- ✅ **Logique métier** : Encapsulée (IsHot, IsCold)

### 7. Rich Domain Entities

```csharp
// Entité avec encapsulation forte
public class ApiKey
{
    public string Key { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    
    // Méthodes métier (pas de setters publics)
    public void Revoke(string reason) { ... }
    public void RecordUsage() { ... }
    public bool IsValid() => IsActive && !IsExpired();
}
```

---

## 📚 Documentation Détaillée

### Configuration

#### Variables d'Environnement

**`.env` (Développement)** :
```env
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_DB=weatherforecastdb
POSTGRES_USER=weatheruser
POSTGRES_PASSWORD=weatherpass
REDIS_PASSWORD=redisSecurePass123!
```

#### Connection Strings

**`appsettings.json`** :
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=weatherforecastdb;Username=weatheruser;Password=weatherpass",
    "Redis": "host.docker.internal:6379,password=redisSecurePass123!"
  }
}
```

**`appsettings.Development.json` (Docker) :
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=weatherforecastdb;Username=weatheruser;Password=weatherpass",
    "Redis": "redis:6379,password=redisSecurePass123!"
  }
}
```

### Endpoints API

#### Authentification

```http
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
```

#### Prévisions Météo (Lecture seule - API Key requise)

```http
GET /api/weatherforecast          # Liste toutes les prévisions
GET /api/weatherforecast/{id}     # Récupère une prévision
```

**Authentification API** :
```http
Authorization: Bearer <api-key>
```

### SignalR Events

Le hub SignalR émet les événements suivants :

```javascript
// Écouter les événements
connection.on("ForecastCreated", (forecast) => { ... });
connection.on("ForecastUpdated", (forecast) => { ... });
connection.on("ForecastDeleted", (id) => { ... });
```

### Redis Channels

```
weatherforecast.created   → Nouvelle prévision créée
weatherforecast.updated   → Prévision mise à jour
weatherforecast.deleted   → Prévision supprimée
```

---

## 🧪 Tests

### 1. Tester les Notifications Temps Réel

**Scénario** : Vérifier que les modifications apparaissent instantanément sur tous les clients connectés.

1. **Ouvrir 2 navigateurs** (ou 2 onglets en navigation privée) sur https://localhost:7203
2. **Créer 2 comptes différents** et se connecter
3. **Navigateur 1** : Créer/Modifier/Supprimer une prévision météo
4. **Navigateur 2** : ✅ **La modification apparaît instantanément sans rafraîchir la page !**

**Vérifications dans la console (F12)** :
```
✅ Connecté au hub SignalR WeatherForecast
🔔 ForecastCreated reçu: { id: 1, date: "2025-10-22", temperatureC: 25 }
```

### 2. Tester l'API REST avec Swagger

1. **Ouvrir** https://localhost:7252/swagger/index.html
2. **Créer une API Key** depuis l'interface Web (https://localhost:7203/ApiKeys)
3. Dans Swagger, cliquer sur **"Authorize"**
4. Entrer l'API Key au format : `Basic <api-key>:<api-secret>`
5. **Tester les endpoints** :
   - `GET /api/weatherforecast` : Liste toutes les prévisions
   - `GET /api/weatherforecast/{id}` : Récupère une prévision

**Exemple de requête** :
```http
GET https://localhost:7252/api/weatherforecast
Authorization: Basic wf_live_abc123:secret_xyz789
```

### 3. Tester Redis Pub/Sub

**Scénario** : Observer les messages Redis en temps réel.

```bash
# Se connecter au container Redis
docker exec -it weatherforecast-redis redis-cli -a redisSecurePass123!

# Écouter un canal
SUBSCRIBE weatherforecast.created

# Dans un autre terminal, créer une prévision via l'interface Web
# → Le message JSON apparaît dans le terminal Redis ✅
```

**Résultat attendu** :
```
1) "message"
2) "weatherforecast.created"
3) "{\"id\":1,\"date\":\"2025-10-22T00:00:00Z\",\"temperatureC\":25,\"summary\":\"Warm\"}"
```

### 4. Tester le Rate Limiting

**Scénario** : Vérifier que le rate limiting fonctionne.

```bash
# Envoyer 101 requêtes rapidement (dépasse la limite de 100/min)
for i in {1..101}; do
  curl -H "Authorization: Basic <api-key>:<secret>" https://localhost:7252/api/weatherforecast
done

# La 101ème requête retourne :
# HTTP 429 Too Many Requests
# { "error": "Rate limit exceeded. Try again in 60 seconds." }
```

### 5. Tester la Session Validation

**Scénario** : Vérifier que le logout forcé fonctionne.

1. **Navigateur 1** : Se connecter en tant qu'utilisateur
2. **Navigateur 2** : Se connecter en tant qu'admin
3. **Navigateur 2** : Révoquer la session de l'utilisateur depuis le dashboard admin
4. **Navigateur 1** : ✅ **Déconnexion automatique avec notification SignalR !**

### 6. Tests Unitaires

**Exécuter tous les tests** :
```bash
cd tests
dotnet test
```

**Résultat attendu** :
```
✅ Passed: 45 tests (100%)
   - Domain.Entities: 12 tests
   - Domain.Services: 15 tests
   - Domain.ValueObjects: 6 tests
   - Infra.Repositories: 8 tests
   - Api.Middleware: 4 tests
```

**Exemples de tests** :
- `WeatherForecastTests` : Validation des entités
- `TemperatureTests` : Validation des Value Objects
- `ApiKeyServiceTests` : Hashing Argon2id
- `WeatherForecastRepositoryTests` : CRUD avec EF Core

---

## 🐛 Dépannage

### PostgreSQL ne démarre pas

```bash
# Vérifier les logs
docker logs weatherforecast-db

# Redémarrer
docker-compose -f docker-compose.dev.yml restart postgres
```

### Redis ne se connecte pas

**Erreur** : `Cannot write DateTime with Kind=Unspecified`

**Solution** : Déjà corrigée dans `Program.cs` :
```csharp
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
```

### SignalR ne reçoit pas les notifications

1. Vérifier que Redis est démarré :
```bash
docker ps | grep redis
```

2. Vérifier les logs de l'application Web :
```
✅ Connecté à Redis: host.docker.internal:6379
🔔 Redis Subscriber Service démarré
✅ Abonné aux canaux Redis
```

3. Vérifier la connexion SignalR dans la console du navigateur (F12) :
```
✅ Connecté au hub SignalR WeatherForecast
```

---

## 🎓 Concepts Avancés

### Flux Complet d'un Événement

```
1. Action Utilisateur
   ↓
2. Controller (API ou Web)
   ↓
3. Service Métier (WeatherForecastService)
   ↓
4. Repository (via UnitOfWork)
   ↓
5. SaveChangesAsync() → Transaction DB
   ↓
6. EventPublisher.Publish(ForecastCreatedEvent)
   ↓
7. Handlers Parallèles (Task.WhenAll) :
   ├─ AuditLogHandler → Logs console avec métriques
   ├─ SignalRHandler → Broadcast direct aux clients connectés
   └─ RedisBrokerHandler → Publie vers Redis Pub/Sub
       ↓
8. Redis Channel (weatherforecast.created)
   ↓
9. RedisSubscriberService (BackgroundService)
   ↓
10. SignalR Hub → Tous les clients (sauf émetteur)
   ↓
11. Clients Web → Mise à jour UI en temps réel
```

### Pourquoi cette Architecture ?

#### 1. **Scalabilité Horizontale**
- API et Web App déployables **séparément**
- Plusieurs instances de chaque application possibles
- Redis Pub/Sub permet la communication entre instances

#### 2. **Résilience**
- Si Redis tombe : Les apps continuent de fonctionner (broadcast direct via SignalR)
- Si une instance tombe : Les autres continuent de servir les requêtes
- Retry automatique sur les connexions Redis

#### 3. **Performance**
- **Handlers parallèles** : Exécution simultanée avec Task.WhenAll
- **DbContext Pooling** : 256 instances réutilisables
- **AsNoTracking** : Requêtes read-only optimisées
- **Index composites** : Recherches rapides en base

#### 4. **Sécurité Multi-Niveaux**
- **Argon2id** : Hashing moderne (OWASP 2024)
- **Rate Limiting** : Protection contre abus (100 req/min)
- **Session Validation** : Vérification DB à chaque requête
- **Brute Force Protection** : 5 tentatives max, blocage 15 minutes

#### 5. **Maintenabilité**
- **Clean Architecture** : Séparation stricte des responsabilités
- **SOLID** : Respect rigoureux des 5 principes
- **DDD** : Entités riches, Value Objects, Domain Events
- **Tests** : Toutes les dépendances mockables

#### 6. **Observabilité**
- **Logs structurés** : Avec corrélation ID
- **Métriques** : Durée d'exécution des handlers
- **Audit complet** : Traçabilité de toutes les actions

### Comparaison avec d'Autres Architectures

| Critère | Architecture Monolithique | Microservices | **Cette Architecture** |
|---------|---------------------------|---------------|------------------------|
| **Complexité** | Faible | Très élevée | Moyenne |
| **Scalabilité** | Limitée | Excellente | Bonne (API + Web séparés) |
| **Temps réel** | Difficile | Complexe | ✅ **Natif (SignalR + Redis)** |
| **Maintenance** | Difficile (couplage) | Complexe (distributed) | ✅ **Facile (Clean Arch)** |
| **Déploiement** | Simple | Complexe | Moyen (2 apps) |
| **Tests** | Difficile | Moyen | ✅ **Facile (DI + Mocking)** |

**Verdict** : Cette architecture offre un **excellent compromis** entre simplicité et fonctionnalités avancées.

---

## 📖 Ressources et Documentation

### Documentation Officielle

- **[ASP.NET Core 8.0](https://docs.microsoft.com/aspnet/core)** - Framework web
- **[Entity Framework Core 8.0](https://docs.microsoft.com/ef/core)** - ORM
- **[SignalR](https://docs.microsoft.com/aspnet/core/signalr)** - WebSocket temps réel
- **[StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)** - Client Redis
- **[PostgreSQL](https://www.postgresql.org/docs/)** - Base de données

### Articles et Guides Recommandés

#### Architecture
- **[Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)** - Uncle Bob Martin
- **[Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)** - Martin Fowler
- **[CQRS Pattern](https://docs.microsoft.com/azure/architecture/patterns/cqrs)** - Microsoft Azure

#### Sécurité
- **[OWASP Top 10 2024](https://owasp.org/www-project-top-ten/)** - Vulnérabilités web
- **[Argon2 Password Hashing](https://github.com/P-H-C/phc-winner-argon2)** - Hashing moderne
- **[ASP.NET Core Security](https://docs.microsoft.com/aspnet/core/security/)** - Best practices

#### Patterns
- **[Domain Events](https://docs.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)** - Microsoft
- **[Repository Pattern](https://docs.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)** - Microsoft
- **[Unit of Work](https://martinfowler.com/eaaCatalog/unitOfWork.html)** - Martin Fowler

### Documentation Interne

- **[doc/architecture/DOMAIN_EVENTS.md](doc/architecture/DOMAIN_EVENTS.md)** - Système d'événements
- **[doc/architecture/REDIS_PUBSUB.md](doc/architecture/REDIS_PUBSUB.md)** - Communication Redis
- **[doc/architecture/NOTIFICATION_SYSTEM.md](doc/architecture/NOTIFICATION_SYSTEM.md)** - Notifications temps réel
- **[doc/code/Readme.md](doc/code/Readme.md)** - Analyse technique complète
- **[doc/production/CHECKLIST-PRODUCTION.md](doc/production/CHECKLIST-PRODUCTION.md)** - Checklist déploiement

---

## 🎯 Prochaines Étapes

### Pour Apprendre

1. **Lire la documentation technique** : `doc/code/Readme.md`
2. **Étudier les Domain Events** : `doc/architecture/DOMAIN_EVENTS.md`
3. **Comprendre Redis Pub/Sub** : `doc/architecture/REDIS_PUBSUB.md`
4. **Analyser les tests** : `tests/` (18 fichiers)

### Pour Développer

1. **Ajouter une nouvelle entité** :
   - Créer l'entité dans `domain/Entities/`
   - Créer le repository dans `infra/Repositories/`
   - Créer le service dans `domain/Services/`
   - Ajouter les événements dans `domain/Events/`

2. **Ajouter un nouveau Hub SignalR** :
   - Créer le hub dans `shared/Hubs/`
   - Ajouter les événements correspondants
   - Créer les handlers dans `application/Handlers/`

3. **Ajouter des tests** :
   - Tests unitaires dans `tests/Domain/`
   - Tests d'intégration dans `tests/Infra/`

### Pour Déployer

1. **Lire le checklist** : `doc/production/CHECKLIST-PRODUCTION.md`
2. **Configurer les secrets** : `doc/production/SETUP-CICD-SECRETS.md`
3. **Générer les certificats** : `doc/production/SETUP-PRODUCTION-CERTIFICATE.md`
4. **Déployer avec Docker Compose** : `docker-compose.yml`

---

## 📝 Licence

Ce projet est fourni à des fins **éducatives et de démonstration**.  
Libre d'utilisation et de modification pour vos propres projets.

---

## 👨‍💻 Contribution

Les contributions sont les bienvenues ! N'hésitez pas à :
- 🐛 Signaler des bugs
- 💡 Proposer des améliorations
- 📖 Améliorer la documentation
- ✨ Ajouter de nouvelles fonctionnalités

---

## 🙏 Remerciements

- **Microsoft** pour ASP.NET Core et Entity Framework Core
- **Stack Exchange** pour StackExchange.Redis
- **La communauté .NET** pour les nombreuses ressources et outils
- **OWASP** pour les recommandations de sécurité

---

## 📊 Statistiques du Projet

- **136 fichiers C#** (~15,000 lignes de code)
- **5 entités riches** avec encapsulation
- **11 services métier** découplés
- **19 événements domaine** avec handlers
- **20+ design patterns** implémentés
- **18 fichiers de tests** (NUnit)
- **3 SignalR Hubs** pour temps réel
- **11 canaux Redis** Pub/Sub

---

**Bon développement ! 🚀**

*Pour toute question, consultez la [documentation technique complète](doc/code/Readme.md).*