# Tests Domain - WeatherForecast

## 📋 Vue d'Ensemble

Ce projet contient les tests unitaires pour la couche **Domain** du projet WeatherForecast. Les tests sont écrits avec **NUnit**, **Moq** et **FluentAssertions**.

## 🧪 Frameworks de Test

| Package | Version | Utilisation |
|---------|---------|-------------|
| **NUnit** | 4.x | Framework de tests unitaires |
| **Moq** | 4.20+ | Mocking des dépendances |
| **FluentAssertions** | 8.8+ | Assertions expressives |
| **NSubstitute** | 5.3+ | Alternative à Moq |

## 📁 Structure des Tests

```
Domain.Tests/
├── Entities/
│   ├── ApplicationUserTests.cs       (30+ tests)
│   ├── ApiKeyTests.cs                 (40+ tests)
│   ├── SessionTests.cs                (25+ tests)
│   └── WeatherForecastTests.cs        (25+ tests)
├── ValueObjects/
│   ├── TemperatureTests.cs            (15+ tests)
│   └── ApiKeyScopesTests.cs           (20+ tests)
└── Services/
    └── WeatherForecastServiceTests.cs (10+ tests)
```

## ✅ Couverture des Tests

### Entités (Entities)

#### ApplicationUser
- ✅ Constructeur avec validation
- ✅ RecordLogin() - Enregistrement connexion
- ✅ Deactivate() / Reactivate() - Gestion activation
- ✅ UpdatePersonalInfo() - Mise à jour infos
- ✅ IsNewUser() / IsInactiveSince() - Propriétés calculées

#### ApiKey
- ✅ Constructeur avec validation complète
- ✅ RecordUsage() - Compteur d'utilisation
- ✅ Revoke() / Reactivate() - Gestion révocation
- ✅ IsValid() / IsExpired() - Validation état
- ✅ HasScope() - Vérification permissions
- ✅ ExtendExpiration() - Prolongation
- ✅ IsIpAllowed() - IP Whitelisting

#### Session
- ✅ Constructeur avec validation dates
- ✅ Revoke() - Révocation avec raison
- ✅ Extend() - Prolongation session
- ✅ IsValid() / IsExpired() - Validation
- ✅ GetRemainingLifetime() - Durée restante
- ✅ IsWebSession() / IsApiSession() - Type session

#### WeatherForecast
- ✅ Constructeur avec Value Object Temperature
- ✅ UpdateTemperature() / UpdateDate() / UpdateSummary()
- ✅ Validation dates (±1 an)
- ✅ IsHot() / IsCold() - Propriétés calculées

### Value Objects

#### Temperature
- ✅ Constructeur avec validation bornes [-100, 100]
- ✅ Conversion Celsius ↔ Fahrenheit
- ✅ Propriétés calculées (IsHot, IsCold)
- ✅ Immutabilité (record)
- ✅ Égalité structurelle
- ✅ FromFahrenheit() - Factory method

#### ApiKeyScopes
- ✅ Validation scopes OAuth2
- ✅ Suppression doublons
- ✅ HasScope() - Vérification
- ✅ Factory methods (ReadOnly, ReadWrite, FullAccess)
- ✅ ToScopeString() - Sérialisation

### Services

#### WeatherForecastService
- ✅ GetAllAsync() - Récupération tous
- ✅ GetByIdAsync() - Récupération par ID
- ✅ CreateAsync() - Création + Event
- ✅ UpdateAsync() - Mise à jour + Event
- ✅ DeleteAsync() - Suppression + Event
- ✅ Vérification publication Domain Events

## 🚀 Exécuter les Tests

### Tous les tests
```bash
dotnet test tests/Domain.Tests/Domain.Tests.csproj
```

### Tests d'une classe spécifique
```bash
dotnet test tests/Domain.Tests/Domain.Tests.csproj --filter "FullyQualifiedName~TemperatureTests"
```

### Tests avec couverture de code
```bash
dotnet test tests/Domain.Tests/Domain.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

### Tests en mode verbeux
```bash
dotnet test tests/Domain.Tests/Domain.Tests.csproj --logger "console;verbosity=detailed"
```

### Tests dans Visual Studio
1. Ouvrir **Test Explorer** (Ctrl+E, T)
2. Cliquer sur **Run All** (Ctrl+R, A)
3. Voir les résultats en temps réel

### Tests avec NUnit Console
```bash
dotnet test tests/Domain.Tests/Domain.Tests.csproj --logger:nunit
```

## 📊 Conventions de Test

### Naming Convention (AAA Pattern)

```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Préparer les données
    var input = "test";
    
    // Act - Exécuter l'action
    var result = MethodUnderTest(input);
    
    // Assert - Vérifier le résultat
    result.Should().Be("expected");
}
```

### FluentAssertions Examples

```csharp
// Égalité
result.Should().Be(expected);

// Collections
list.Should().HaveCount(3);
list.Should().Contain(item);
list.Should().BeEquivalentTo(expectedList);

// Exceptions
Action act = () => ThrowingMethod();
act.Should().Throw<ArgumentException>()
    .WithMessage("*specific text*");

// DateTime
date.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

// Nullabilité
result.Should().NotBeNull();
result.Should().BeNull();

// Booléens
flag.Should().BeTrue();
flag.Should().BeFalse();
```

### Moq Examples

```csharp
// Setup - Simuler comportement
mockRepository.Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(expectedObject);

// Verify - Vérifier appel
mockRepository.Verify(r => r.AddAsync(It.IsAny<Entity>()), Times.Once);

// Match arguments
mockPublisher.Verify(p => p.Publish(
    It.Is<Event>(e => e.Id == 1),
    default), Times.Once);
```

## 📈 Statistiques Tests

| Catégorie | Nombre de Tests | Status |
|-----------|-----------------|--------|
| **Entities** | ~120 tests | ✅ Complet |
| **Value Objects** | ~35 tests | ✅ Complet |
| **Services** | ~10 tests | 🟡 En cours |
| **Total** | **~165 tests** | 🔄 Evolution |

## 🎯 Tests à Ajouter (Roadmap)

### Priorité HAUTE
- [ ] **ApiKeyService** - Tests complets avec mocking
- [ ] **UserManagementService** - CRUD utilisateurs
- [ ] **SessionManagementService** - Gestion sessions
- [ ] **AuthenticationService** - Login/Register

### Priorité MOYENNE
- [ ] **RoleManagementService** - Gestion rôles
- [ ] **JwtService** - Génération tokens
- [ ] **RateLimitService** - Rate limiting

### Priorité BASSE
- [ ] Tests de performance (benchmarks)
- [ ] Tests de charge (stress tests)

## 🐛 Debugging Tests

### En cas d'échec

```bash
# Lister les tests
dotnet test --list-tests

# Exécuter un test spécifique
dotnet test --filter "Name=Constructor_WithValidParameters_ShouldCreateTemperature"

# Voir les logs détaillés
dotnet test --logger "console;verbosity=normal"
```

### Dans Visual Studio

1. Mettre un breakpoint dans le test
2. Clic droit → **Debug Test**
3. Inspecter les valeurs avec Watch Window

## 📝 Bonnes Pratiques

### ✅ À FAIRE

- ✅ Tester tous les cas nominaux
- ✅ Tester tous les cas d'erreur
- ✅ Tester les cas limites (boundary)
- ✅ Tester les validations
- ✅ Mocker les dépendances externes
- ✅ Utiliser [SetUp] pour initialisation commune
- ✅ Utiliser [TestCase] pour tests paramétrés
- ✅ Nommer clairement les tests (AAA)

### ❌ À ÉVITER

- ❌ Tests dépendants (ordre d'exécution)
- ❌ Tests qui modifient l'état global
- ❌ Tests trop longs (>500ms)
- ❌ Plusieurs assertions non liées
- ❌ Tester des détails d'implémentation
- ❌ Ignorer les tests ([Ignore])

## 🔗 Ressources

- [NUnit Documentation](https://docs.nunit.org/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions](https://fluentassertions.com/)
- [Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

---

**Dernière mise à jour:** 23 Octobre 2025  
**Mainteneur:** DevOps Team  
**Coverage:** ~165 tests | **Status:** ✅ En production
