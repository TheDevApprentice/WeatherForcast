using domain.Constants;
using domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace tests.Domain.ValueObjects
{
    [TestFixture]
    public class ApiKeyScopesTests
    {
        [Test]
        public void Constructor_WithValidSingleScope_ShouldCreate()
        {
            // Arrange & Act
            var scopes = new ApiKeyScopes(AppClaims.ForecastRead);

            // Assert
            scopes.Scopes.Should().ContainSingle();
            scopes.Scopes.Should().Contain(AppClaims.ForecastRead);
        }

        [Test]
        public void Constructor_WithMultipleScopes_ShouldCreate()
        {
            // Arrange
            var scopeString = $"{AppClaims.ForecastRead} {AppClaims.ForecastWrite}";

            // Act
            var scopes = new ApiKeyScopes(scopeString);

            // Assert
            scopes.Scopes.Should().HaveCount(2);
            scopes.Scopes.Should().Contain(AppClaims.ForecastRead);
            scopes.Scopes.Should().Contain(AppClaims.ForecastWrite);
        }

        [Test]
        public void Constructor_WithDuplicateScopes_ShouldRemoveDuplicates()
        {
            // Arrange
            var scopeString = $"{AppClaims.ForecastRead} {AppClaims.ForecastRead} {AppClaims.ForecastWrite}";

            // Act
            var scopes = new ApiKeyScopes(scopeString);

            // Assert
            scopes.Scopes.Should().HaveCount(2);
            scopes.Scopes.Should().Contain(AppClaims.ForecastRead);
            scopes.Scopes.Should().Contain(AppClaims.ForecastWrite);
        }

        [Test]
        public void Constructor_WithInvalidScope_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidScope = "invalid:scope";

            // Act
            Action act = () => new ApiKeyScopes(invalidScope);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Scopes invalides*");
        }

        [Test]
        public void Constructor_WithEmptyString_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApiKeyScopes("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Les scopes ne peuvent pas être vides*");
        }

        [Test]
        public void Constructor_WithWhitespace_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApiKeyScopes("   ");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void HasScope_WithExistingScope_ShouldReturnTrue()
        {
            // Arrange
            var scopes = new ApiKeyScopes($"{AppClaims.ForecastRead} {AppClaims.ForecastWrite}");

            // Act & Assert
            scopes.HasScope(AppClaims.ForecastRead).Should().BeTrue();
            scopes.HasScope(AppClaims.ForecastWrite).Should().BeTrue();
        }

        [Test]
        public void HasScope_WithNonExistingScope_ShouldReturnFalse()
        {
            // Arrange
            var scopes = new ApiKeyScopes(AppClaims.ForecastRead);

            // Act & Assert
            scopes.HasScope(AppClaims.ForecastWrite).Should().BeFalse();
            scopes.HasScope(AppClaims.ForecastDelete).Should().BeFalse();
        }

        [Test]
        public void ReadOnly_ShouldContainOnlyReadScope()
        {
            // Act
            var scopes = ApiKeyScopes.ReadOnly;

            // Assert
            scopes.Scopes.Should().ContainSingle();
            scopes.Scopes.Should().Contain(AppClaims.ForecastRead);
            scopes.HasScope(AppClaims.ForecastRead).Should().BeTrue();
            scopes.HasScope(AppClaims.ForecastWrite).Should().BeFalse();
        }

        [Test]
        public void ReadWrite_ShouldContainReadAndWriteScopes()
        {
            // Act
            var scopes = ApiKeyScopes.ReadWrite;

            // Assert
            scopes.Scopes.Should().HaveCount(2);
            scopes.HasScope(AppClaims.ForecastRead).Should().BeTrue();
            scopes.HasScope(AppClaims.ForecastWrite).Should().BeTrue();
            scopes.HasScope(AppClaims.ForecastDelete).Should().BeFalse();
        }

        [Test]
        public void FullAccess_ShouldContainAllScopes()
        {
            // Act
            var scopes = ApiKeyScopes.FullAccess;

            // Assert
            scopes.Scopes.Should().HaveCount(3);
            scopes.HasScope(AppClaims.ForecastRead).Should().BeTrue();
            scopes.HasScope(AppClaims.ForecastWrite).Should().BeTrue();
            scopes.HasScope(AppClaims.ForecastDelete).Should().BeTrue();
        }

        [Test]
        public void ToScopeString_ShouldReturnSpaceSeparatedString()
        {
            // Arrange
            var scopes = ApiKeyScopes.ReadWrite;

            // Act
            var result = scopes.ToScopeString();

            // Assert
            result.Should().Contain(AppClaims.ForecastRead);
            result.Should().Contain(AppClaims.ForecastWrite);
            result.Should().Contain(" "); // Space separator
        }

        [Test]
        public void Equality_WithSameScopes_ShouldBeEqual()
        {
            // Arrange
            var scopes1 = new ApiKeyScopes($"{AppClaims.ForecastRead} {AppClaims.ForecastWrite}");
            var scopes2 = new ApiKeyScopes($"{AppClaims.ForecastRead} {AppClaims.ForecastWrite}");

            // Act & Assert
            scopes1.Should().Be(scopes2);
        }

        [Test]
        public void Equality_WithDifferentScopes_ShouldNotBeEqual()
        {
            // Arrange
            var scopes1 = ApiKeyScopes.ReadOnly;
            var scopes2 = ApiKeyScopes.ReadWrite;

            // Act & Assert
            scopes1.Should().NotBe(scopes2);
        }
    }
}
