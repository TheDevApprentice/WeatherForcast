using domain.Entities;
using domain.ValueObjects;
using domain.Constants;
using FluentAssertions;
using NUnit.Framework;

namespace tests.Domain.Entities
{
    [TestFixture]
    public class ApiKeyTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateApiKey()
        {
            // Arrange
            var name = "Test API Key";
            var key = "wf_live_test123";
            var secretHash = "hashed_secret";
            var userId = "user123";
            var scopes = ApiKeyScopes.ReadWrite;

            // Act
            var apiKey = new ApiKey(name, key, secretHash, userId, scopes);

            // Assert
            apiKey.Name.Should().Be(name);
            apiKey.Key.Should().Be(key);
            apiKey.SecretHash.Should().Be(secretHash);
            apiKey.UserId.Should().Be(userId);
            apiKey.Scopes.Should().Be(scopes);
            apiKey.IsActive.Should().BeTrue();
            apiKey.RequestCount.Should().Be(0);
            apiKey.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Constructor_WithEmptyName_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApiKey("", "key", "hash", "user", ApiKeyScopes.ReadOnly);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le nom de la clé est requis*");
        }

        [Test]
        public void Constructor_WithEmptyKey_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApiKey("name", "", "hash", "user", ApiKeyScopes.ReadOnly);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*La clé API est requise*");
        }

        [Test]
        public void Constructor_WithEmptySecretHash_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApiKey("name", "key", "", "user", ApiKeyScopes.ReadOnly);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le hash du secret est requis*");
        }

        [Test]
        public void Constructor_WithEmptyUserId_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApiKey("name", "key", "hash", "", ApiKeyScopes.ReadOnly);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*L'utilisateur propriétaire est requis*");
        }

        [Test]
        public void RecordUsage_WhenActive_ShouldIncrementCountAndUpdateTimestamp()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);
            var initialCount = apiKey.RequestCount;

            // Act
            apiKey.RecordUsage();

            // Assert
            apiKey.RequestCount.Should().Be(initialCount + 1);
            apiKey.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void RecordUsage_WhenRevoked_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);
            apiKey.Revoke("Test revocation");

            // Act
            Action act = () => apiKey.RecordUsage();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Impossible d'utiliser une clé révoquée*");
        }

        [Test]
        public void RecordUsage_WhenExpired_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var expiresAt = DateTime.UtcNow.AddSeconds(1);
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly, expiresAt);
            Thread.Sleep(1100); // Wait for expiration

            // Act
            Action act = () => apiKey.RecordUsage();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*La clé API a expiré*");
        }

        [Test]
        public void Revoke_WithReason_ShouldRevokeApiKey()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);
            var reason = "Security breach";

            // Act
            apiKey.Revoke(reason);

            // Assert
            apiKey.IsActive.Should().BeFalse();
            apiKey.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            apiKey.RevocationReason.Should().Be(reason);
            apiKey.IsRevoked.Should().BeTrue();
        }

        [Test]
        public void Revoke_WithoutReason_ShouldThrowArgumentException()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);

            // Act
            Action act = () => apiKey.Revoke("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Une raison de révocation est requise*");
        }

        [Test]
        public void Revoke_WhenAlreadyRevoked_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);
            apiKey.Revoke("First revocation");

            // Act
            Action act = () => apiKey.Revoke("Second revocation");

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*La clé est déjà révoquée*");
        }

        [Test]
        public void Reactivate_WhenRevoked_ShouldReactivateApiKey()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);
            apiKey.Revoke("Test");

            // Act
            apiKey.Reactivate();

            // Assert
            apiKey.IsActive.Should().BeTrue();
            apiKey.RevokedAt.Should().BeNull();
            apiKey.RevocationReason.Should().BeNull();
        }

        [Test]
        public void Reactivate_WhenActive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);

            // Act
            Action act = () => apiKey.Reactivate();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*La clé est déjà active*");
        }

        [Test]
        public void IsExpired_WithoutExpirationDate_ShouldReturnFalse()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);

            // Act & Assert
            apiKey.IsExpired().Should().BeFalse();
        }

        [Test]
        public void IsExpired_WithFutureExpirationDate_ShouldReturnFalse()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly, DateTime.UtcNow.AddDays(30));

            // Act & Assert
            apiKey.IsExpired().Should().BeFalse();
        }

        [Test]
        public void IsValid_WhenActiveAndNotExpired_ShouldReturnTrue()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);

            // Act & Assert
            apiKey.IsValid().Should().BeTrue();
        }

        [Test]
        public void IsValid_WhenRevoked_ShouldReturnFalse()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);
            apiKey.Revoke("Test");

            // Act & Assert
            apiKey.IsValid().Should().BeFalse();
        }

        [Test]
        public void HasScope_WithExistingScope_ShouldReturnTrue()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadWrite);

            // Act & Assert
            apiKey.HasScope(AppClaims.ForecastRead).Should().BeTrue();
            apiKey.HasScope(AppClaims.ForecastWrite).Should().BeTrue();
        }

        [Test]
        public void HasScope_WithNonExistingScope_ShouldReturnFalse()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);

            // Act & Assert
            apiKey.HasScope(AppClaims.ForecastWrite).Should().BeFalse();
            apiKey.HasScope(AppClaims.ForecastDelete).Should().BeFalse();
        }

        [Test]
        public void UpdateScopes_WithValidScopes_ShouldUpdateScopes()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);
            var newScopes = ApiKeyScopes.FullAccess;

            // Act
            apiKey.UpdateScopes(newScopes);

            // Assert
            apiKey.Scopes.Should().Be(newScopes);
            apiKey.HasScope(AppClaims.ForecastRead).Should().BeTrue();
            apiKey.HasScope(AppClaims.ForecastWrite).Should().BeTrue();
            apiKey.HasScope(AppClaims.ForecastDelete).Should().BeTrue();
        }

        [Test]
        public void ExtendExpiration_WithPositiveDuration_ShouldExtendDate()
        {
            // Arrange
            var initialExpiration = DateTime.UtcNow.AddDays(30);
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly, initialExpiration);

            // Act
            apiKey.ExtendExpiration(TimeSpan.FromDays(30));

            // Assert
            apiKey.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(60), TimeSpan.FromSeconds(2));
        }

        [Test]
        public void ExtendExpiration_WithNegativeDuration_ShouldThrowArgumentException()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);

            // Act
            Action act = () => apiKey.ExtendExpiration(TimeSpan.FromDays(-1));

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*La durée doit être positive*");
        }

        [Test]
        public void IsIpAllowed_WithNoRestriction_ShouldReturnTrue()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);

            // Act & Assert
            apiKey.IsIpAllowed("192.168.1.1").Should().BeTrue();
            apiKey.IsIpAllowed("10.0.0.1").Should().BeTrue();
        }

        [Test]
        public void IsIpAllowed_WithRestriction_ShouldValidateIp()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);
            apiKey.SetAllowedIpAddress("192.168.1.1");

            // Act & Assert
            apiKey.IsIpAllowed("192.168.1.1").Should().BeTrue();
            apiKey.IsIpAllowed("10.0.0.1").Should().BeFalse();
        }

        [Test]
        public void UpdateName_WithValidName_ShouldUpdateName()
        {
            // Arrange
            var apiKey = new ApiKey("Old Name", "key", "hash", "user", ApiKeyScopes.ReadOnly);

            // Act
            apiKey.UpdateName("New Name");

            // Assert
            apiKey.Name.Should().Be("New Name");
        }

        [Test]
        public void UpdateName_WithEmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);

            // Act
            Action act = () => apiKey.UpdateName("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le nom ne peut pas être vide*");
        }

        [Test]
        public void SetExpiration_WithFutureDate_ShouldSetExpiration()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);
            var futureDate = DateTime.UtcNow.AddMonths(6);

            // Act
            apiKey.SetExpiration(futureDate);

            // Assert
            apiKey.ExpiresAt.Should().Be(futureDate);
        }

        [Test]
        public void SetExpiration_WithPastDate_ShouldThrowArgumentException()
        {
            // Arrange
            var apiKey = new ApiKey("name", "key", "hash", "user", ApiKeyScopes.ReadOnly);
            var pastDate = DateTime.UtcNow.AddDays(-1);

            // Act
            Action act = () => apiKey.SetExpiration(pastDate);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*La date d'expiration ne peut pas être dans le passé*");
        }
    }
}
