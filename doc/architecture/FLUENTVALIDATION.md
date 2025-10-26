# ✅ FluentValidation - Implémentation Complète

## 📊 Vue d'Ensemble

La solution utilise **FluentValidation** pour toute la validation au niveau présentation (ViewModels/DTOs), offrant une architecture propre, maintenable et testable.

---

## 📝 Validators Créés

### Application (Web MVC)

| Validator | Cible | Validations |
|-----------|-------|-------------|
| **WeatherForecastViewModelValidator** | `WeatherForecastViewModel` | Date (-1 an à +1 an), Summary (pas vide, pas placeholder), TemperatureC (-100 à 100) |
| **CreateApiKeyRequestValidator** | `CreateApiKeyRequest` | Name (pas vide, max 100, alphanumérique), ExpirationDays (positif, max 365) |
| **RegisterViewModelValidator** | `RegisterViewModel` | FirstName/LastName (pas vide, max 50, lettres), Email (valide, max 256), Password (min 6, majuscule, minuscule, chiffre, spécial), ConfirmPassword (égal à Password) |
| **LoginViewModelValidator** | `LoginViewModel` | Email (pas vide, valide), Password (pas vide) |
| **CreateUserViewModelValidator** | `CreateUserViewModel` | FirstName/LastName (pas vide, max 50, lettres), Email (valide, max 256), Password (min 6), SelectedRoles (au moins 1), CustomClaims (cohérents) |

### API (REST)

| Validator | Cible | Validations |
|-----------|-------|-------------|
| **CreateWeatherForecastRequestValidator** | `CreateWeatherForecastRequest` | Date (-1 an à +1 an), Summary (pas vide, max 100), TemperatureC (-100 à 100) |
| **UpdateWeatherForecastRequestValidator** | `UpdateWeatherForecastRequest` | Date (-1 an à +1 an), Summary (pas vide, max 100), TemperatureC (-100 à 100) |
| **RegisterRequestValidator** | `RegisterRequest` | FirstName/LastName (pas vide, max 50, lettres), Email (valide, max 256), Password (min 6, majuscule, minuscule, chiffre, spécial) |
| **LoginRequestValidator** | `LoginRequest` | Email (pas vide, valide), Password (pas vide) |

---

## 🏗️ Architecture de Validation

### Séparation des Responsabilités

**Validation Présentation (FluentValidation)** :
- ViewModels (Application Web)
- DTOs (API REST)
- Feedback utilisateur immédiat
- Messages d'erreur personnalisés

**Validation Domain (Constructeurs/Méthodes)** :
- Intégrité des entités
- Invariants métier
- Protection contre états invalides
- Exceptions typées (ArgumentException, etc.)

### Domain Layer

**`domain/Entities/WeatherForecast.cs`** :
- ✅ Validation `ArgumentNullException` pour Temperature (intégrité domain)
- ✅ Pas de validation de présentation (déléguée à FluentValidation)

**`domain/Services/ApiKeyService.cs`** :
- ✅ Validation déléguée à FluentValidation pour les données de présentation
- ✅ Validation métier conservée (logique business)

**`domain/Entities/ApplicationUser.cs`** :
- ✅ Toutes les validations d'intégrité conservées (DDD)

---

### Application Layer (Web MVC)

#### Controllers

Tous les controllers utilisent le pattern suivant :

```csharp
if (!ModelState.IsValid)
{
    // Publier erreur pour notification SignalR
    await _publisher.PublishValidationErrorAsync(...);
    return View(viewModel);
}
```

**Controllers concernés** :
- `WeatherForecastController` (Create, Edit)
- `ApiKeysController` (Create)
- `AuthController` (Register, Login)
- `AdminController` (Create)

#### ViewModels

Les ViewModels n'utilisent **aucune DataAnnotation de validation** :
- `RegisterViewModel` : Pas de `[Required]`, `[EmailAddress]`, etc.
- `LoginViewModel` : Pas de `[Required]`, `[EmailAddress]`
- `CreateUserViewModel` : Pas de `[Required]`, `[StringLength]`
- Seuls `[Display]` et `[DataType]` sont conservés (affichage uniquement)

---

### API Layer (REST)

#### DTOs

Les DTOs n'utilisent **aucune DataAnnotation de validation** :
- `RegisterRequest` : Validation via `RegisterRequestValidator`
- `LoginRequest` : Validation via `LoginRequestValidator`
- `CreateWeatherForecastRequest` : Validation via `CreateWeatherForecastRequestValidator`
- `UpdateWeatherForecastRequest` : Validation via `UpdateWeatherForecastRequestValidator`

---

## 🎯 Pattern de Validation Implémenté

### 1. **Controllers (Application + API)**

```csharp
[HttpPost]
public async Task<IActionResult> Create(WeatherForecastViewModel viewModel)
{
    // ✅ Validation FluentValidation via ModelState
    if (!ModelState.IsValid)
    {
        // Publier l'erreur pour notification SignalR (Application uniquement)
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

        return View(viewModel); // ou BadRequest(ModelState) pour API
    }

    // ... logique métier
}
```

### 2. **Validators**

#### Validation de Date (Must pour compatibilité client-side)

```csharp
RuleFor(x => x.Date)
    .Must(date => date.Date >= DateTime.UtcNow.Date.AddYears(-1))
    .WithMessage("La date ne peut pas être antérieure à 1 an")
    .Must(date => date.Date <= DateTime.UtcNow.Date.AddYears(1))
    .WithMessage("La date ne peut pas être supérieure à 1 an dans le futur");
```

**Note** : Utilisation de `.Must()` au lieu de `.GreaterThanOrEqualTo()` sur `.Date.Date` pour éviter les problèmes de sérialisation JavaScript côté client.

#### Validation de Password (Must pour éviter validation agressive)

```csharp
RuleFor(x => x.Password)
    .NotEmpty()
    .WithMessage("Le mot de passe est requis")
    .MinimumLength(6)
    .WithMessage("Le mot de passe doit contenir au moins 6 caractères")
    .Must(password => string.IsNullOrEmpty(password) || 
        (password.Any(char.IsUpper) && 
         password.Any(char.IsLower) && 
         password.Any(char.IsDigit) && 
         password.Any(ch => !char.IsLetterOrDigit(ch))))
    .WithMessage("Le mot de passe doit contenir au moins une majuscule, une minuscule, un chiffre et un caractère spécial");
```

**Note** : Utilisation de `.Must()` avec une seule condition combinée au lieu de multiples `.Matches()` pour éviter la validation agressive pendant la saisie.

---

## 📋 Règles de Validation Détaillées

### WeatherForecast

| Champ | Règles |
|-------|--------|
| **Date** | Entre -1 an et +1 an |
| **Summary** | Pas vide, pas "-- Sélectionnez --" |
| **TemperatureC** | Entre -100°C et 100°C |

### ApiKey

| Champ | Règles |
|-------|--------|
| **Name** | Pas vide, max 100 caractères, alphanumérique + espaces/tirets/underscores |
| **ExpirationDays** | Positif, max 365 jours (si fourni) |

### User (Register)

| Champ | Règles |
|-------|--------|
| **FirstName** | Pas vide, max 50 caractères, lettres + espaces/apostrophes/tirets |
| **LastName** | Pas vide, max 50 caractères, lettres + espaces/apostrophes/tirets |
| **Email** | Pas vide, format email valide, max 256 caractères |
| **Password** | Min 6 caractères, max 100, au moins 1 majuscule, 1 minuscule, 1 chiffre, 1 caractère spécial |
| **ConfirmPassword** | Égal à Password |

### User (Login)

| Champ | Règles |
|-------|--------|
| **Email** | Pas vide, format email valide |
| **Password** | Pas vide |

### User (Admin Create)

| Champ | Règles |
|-------|--------|
| **FirstName** | Pas vide, max 50 caractères, lettres + espaces/apostrophes/tirets |
| **LastName** | Pas vide, max 50 caractères, lettres + espaces/apostrophes/tirets |
| **Email** | Pas vide, format email valide, max 256 caractères |
| **Password** | Min 6 caractères, max 100 |
| **SelectedRoles** | Au moins 1 rôle sélectionné |
| **CustomClaims** | Type et Valeur cohérents (si l'un est fourni, l'autre aussi) |

---

## ⚠️ Points Importants

### 1. **Double Validation (Defense in Depth)**

**ViewModel/DTO** : FluentValidation (feedback utilisateur)
```csharp
RuleFor(x => x.TemperatureC).InclusiveBetween(-100, 100);
```

**Domain** : Validation constructeur (intégrité)
```csharp
public Temperature(int celsius)
{
    if (celsius < -100 || celsius > 100)
        throw new ArgumentException("...");
}
```

### 2. **Notifications SignalR**

FluentValidation ne lève pas d'exception → Publication manuelle si `!ModelState.IsValid`

```csharp
if (!ModelState.IsValid)
{
    var errors = string.Join(", ", ModelState.Values
        .SelectMany(v => v.Errors)
        .Select(e => e.ErrorMessage));
    
    await _publisher.PublishValidationErrorAsync(User, errors, "Create", "WeatherForecast", null, null);
    
    return View(viewModel);
}
```

### 3. **Validation Côté Client**

Après configuration de `FluentValidation.AspNetCore` avec `AddFluentValidationClientsideAdapters()`, la validation JavaScript sera générée automatiquement.

---

## 🎉 Avantages Obtenus

### 1. **Séparation des Responsabilités**
- ✅ Validation présentation : Validators FluentValidation
- ✅ Validation domain : Constructeurs et méthodes

### 2. **Réutilisabilité**
- ✅ Validators utilisables dans API + Web App
- ✅ Règles centralisées

### 3. **Lisibilité**
- ✅ Règles déclaratives claires
- ✅ Messages d'erreur personnalisables

### 4. **Testabilité**
- ✅ Validators testables unitairement
- ✅ Isolation des règles métier

### 5. **Maintenabilité**
- ✅ Modification centralisée des règles
- ✅ Pas de duplication de code

### 6. **Validation Client-Side**
- ✅ Génération JavaScript automatique
- ✅ Feedback immédiat pour l'utilisateur

---

## 📦 Configuration

### Packages NuGet

```xml
<!-- application/application.csproj & api/api.csproj -->
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
```

### Program.cs (Application)

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;

// 8. MVC avec FluentValidation
builder.Services.AddControllersWithViews();

// FluentValidation - Validation automatique
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

**Note** : `AddFluentValidationClientsideAdapters()` génère la validation JavaScript côté client.

### Program.cs (API)

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;

// 8. Controllers avec FluentValidation
builder.Services.AddControllers();

// FluentValidation - Validation automatique
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

**Note** : Pas de `AddFluentValidationClientsideAdapters()` pour l'API (pas de client JavaScript).

---

## 📊 Résumé Technique

### Validators Implémentés

**Application (5)** :
- `WeatherForecastViewModelValidator`
- `CreateApiKeyRequestValidator`
- `RegisterViewModelValidator`
- `LoginViewModelValidator`
- `CreateUserViewModelValidator`

**API (5)** :
- `CreateWeatherForecastRequestValidator`
- `UpdateWeatherForecastRequestValidator`
- `RegisterRequestValidator`
- `LoginRequestValidator`

---