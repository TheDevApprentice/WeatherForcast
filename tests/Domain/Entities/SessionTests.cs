using domain.Entities;
using FluentAssertions;

namespace tests.Domain.Entities
{
    [TestFixture]
    public class SessionTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateSession()
        {
            // Arrange
            var token = "test-token";
            var expiresAt = DateTime.UtcNow.AddDays(7);
            var ipAddress = "192.168.1.1";
            var userAgent = "Mozilla/5.0";

            // Act
            var session = new Session(token, SessionType.Web, expiresAt, ipAddress, userAgent);

            // Assert
            session.Token.Should().Be(token);
            session.Type.Should().Be(SessionType.Web);
            session.ExpiresAt.Should().Be(expiresAt);
            session.IpAddress.Should().Be(ipAddress);
            session.UserAgent.Should().Be(userAgent);
            session.IsRevoked.Should().BeFalse();
            session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Constructor_WithEmptyToken_ShouldThrowArgumentException()
        {
            // Arrange
            var expiresAt = DateTime.UtcNow.AddDays(7);

            // Act
            Action act = () => new Session("", SessionType.Web, expiresAt);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le token est requis*");
        }

        [Test]
        public void Constructor_WithPastExpirationDate_ShouldThrowArgumentException()
        {
            // Arrange
            var pastDate = DateTime.UtcNow.AddDays(-1);

            // Act
            Action act = () => new Session("token", SessionType.Web, pastDate);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*La date d'expiration doit être dans le futur*");
        }

        [Test]
        public void IsExpired_WithFutureDate_ShouldReturnFalse()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddDays(7));

            // Act & Assert
            session.IsExpired().Should().BeFalse();
        }

        [Test]
        public void IsExpired_WithPastDate_ShouldReturnTrue()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddSeconds(1));
            Thread.Sleep(1100); // Wait for expiration

            // Act & Assert
            session.IsExpired().Should().BeTrue();
        }

        [Test]
        public void IsValid_WhenNotRevokedAndNotExpired_ShouldReturnTrue()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddDays(7));

            // Act & Assert
            session.IsValid().Should().BeTrue();
        }

        [Test]
        public void IsValid_WhenRevoked_ShouldReturnFalse()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddDays(7));
            session.Revoke("Test revocation");

            // Act & Assert
            session.IsValid().Should().BeFalse();
        }

        [Test]
        public void Revoke_WithoutReason_ShouldRevokeSession()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddDays(7));

            // Act
            session.Revoke();

            // Assert
            session.IsRevoked.Should().BeTrue();
            session.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Revoke_WithReason_ShouldStoreReason()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddDays(7));
            var reason = "Security breach detected";

            // Act
            session.Revoke(reason);

            // Assert
            session.IsRevoked.Should().BeTrue();
            session.RevocationReason.Should().Be(reason);
            session.RevokedAt.Should().NotBeNull();
        }

        [Test]
        public void Revoke_WhenAlreadyRevoked_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddDays(7));
            session.Revoke("First revocation");

            // Act
            Action act = () => session.Revoke("Second revocation");

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*La session est déjà révoquée*");
        }

        [Test]
        public void Extend_WhenRevoked_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddDays(7));
            session.Revoke();

            // Act
            Action act = () => session.Extend(TimeSpan.FromDays(7));

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Impossible de prolonger une session révoquée*");
        }

        [Test]
        public void GetRemainingLifetime_WhenValid_ShouldReturnTimeSpan()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddDays(7));

            // Act
            var remaining = session.GetRemainingLifetime();

            // Assert
            remaining.Should().NotBeNull();
            remaining.Value.TotalDays.Should().BeApproximately(7, 0.1);
        }

        [Test]
        public void GetRemainingLifetime_WhenRevoked_ShouldReturnNull()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddDays(7));
            session.Revoke();

            // Act
            var remaining = session.GetRemainingLifetime();

            // Assert
            remaining.Should().BeNull();
        }

        [Test]
        public void IsWebSession_ForWebType_ShouldReturnTrue()
        {
            // Arrange
            var session = new Session("token", SessionType.Web, DateTime.UtcNow.AddDays(7));

            // Act & Assert
            session.IsWebSession().Should().BeTrue();
            session.IsApiSession().Should().BeFalse();
        }

        [Test]
        public void IsApiSession_ForApiType_ShouldReturnTrue()
        {
            // Arrange
            var session = new Session("token", SessionType.Api, DateTime.UtcNow.AddDays(1));

            // Act & Assert
            session.IsApiSession().Should().BeTrue();
            session.IsWebSession().Should().BeFalse();
        }
    }
}
