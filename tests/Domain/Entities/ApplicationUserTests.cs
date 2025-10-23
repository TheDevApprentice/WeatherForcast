using domain.Entities;
using FluentAssertions;

namespace tests.Domain.Entities
{
    [TestFixture]
    public class ApplicationUserTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateUser()
        {
            // Arrange
            var email = "test@example.com";
            var firstName = "John";
            var lastName = "Doe";

            // Act
            var user = new ApplicationUser(email, firstName, lastName);

            // Assert
            user.Email.Should().Be(email);
            user.UserName.Should().Be(email);
            user.FirstName.Should().Be(firstName);
            user.LastName.Should().Be(lastName);
            user.IsActive.Should().BeTrue();
            user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            user.LastLoginAt.Should().BeNull();
        }

        [Test]
        public void Constructor_WithNullEmail_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApplicationUser(null!, "John", "Doe");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*L'email est requis*");
        }

        [Test]
        public void Constructor_WithEmptyEmail_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApplicationUser("", "John", "Doe");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*L'email est requis*");
        }

        [Test]
        public void Constructor_WithWhitespaceEmail_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApplicationUser("   ", "John", "Doe");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*L'email est requis*");
        }

        [Test]
        public void Constructor_WithNullFirstName_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApplicationUser("test@example.com", null!, "Doe");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le prénom est requis*");
        }

        [Test]
        public void Constructor_WithEmptyFirstName_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApplicationUser("test@example.com", "", "Doe");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le prénom est requis*");
        }

        [Test]
        public void Constructor_WithWhitespaceFirstName_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApplicationUser("test@example.com", "   ", "Doe");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le prénom est requis*");
        }

        [Test]
        public void Constructor_WithNullLastName_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApplicationUser("test@example.com", "John", null!);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le nom est requis*");
        }

        [Test]
        public void Constructor_WithEmptyLastName_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApplicationUser("test@example.com", "John", "");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le nom est requis*");
        }

        [Test]
        public void Constructor_WithWhitespaceLastName_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => new ApplicationUser("test@example.com", "John", "   ");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le nom est requis*");
        }

        [Test]
        public void FullName_ShouldReturnConcatenatedName()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            // Act
            var fullName = user.FullName;

            // Assert
            fullName.Should().Be("John Doe");
        }

        [Test]
        public void UpdatePersonalInfo_WithValidData_ShouldUpdateInfo()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            var newFirstName = "Jane";
            var newLastName = "Smith";

            // Act
            user.UpdatePersonalInfo(newFirstName, newLastName);

            // Assert
            user.FirstName.Should().Be(newFirstName);
            user.LastName.Should().Be(newLastName);
            user.FullName.Should().Be("Jane Smith");
        }

        [Test]
        public void UpdatePersonalInfo_WithInvalidFirstName_ShouldThrowArgumentException()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            // Act
            Action act = () => user.UpdatePersonalInfo("", "Smith");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le prénom est requis*");
        }

        [Test]
        public void UpdatePersonalInfo_WithInvalidLastName_ShouldThrowArgumentException()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            // Act
            Action act = () => user.UpdatePersonalInfo("Jane", "");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le nom est requis*");
        }

        [Test]
        public void RecordLogin_WhenActive_ShouldUpdateLastLoginAt()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            // Act
            user.RecordLogin();

            // Assert
            user.LastLoginAt.Should().NotBeNull();
            user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Test]
        public void RecordLogin_WhenInactive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            user.Deactivate("Test deactivation");

            // Act
            Action act = () => user.RecordLogin();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Impossible de se connecter : le compte est désactivé*");
        }

        [Test]
        public void Deactivate_WithReason_ShouldDeactivateUser()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            var reason = "Violation of terms";

            // Act
            user.Deactivate(reason);

            // Assert
            user.IsActive.Should().BeFalse();
            user.DeactivatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            user.DeactivationReason.Should().Be(reason);
        }

        [Test]
        public void Deactivate_WithoutReason_ShouldUseDefaultReason()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            // Act
            user.Deactivate();

            // Assert
            user.IsActive.Should().BeFalse();
            user.DeactivationReason.Should().Be("Désactivé par un administrateur");
        }

        [Test]
        public void Deactivate_WhenAlreadyInactive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            user.Deactivate("First deactivation");

            // Act
            Action act = () => user.Deactivate("Second deactivation");

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Le compte est déjà désactivé*");
        }

        [Test]
        public void Deactivate_WithEmptyReason_ShouldThrowArgumentException()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            // Act
            Action act = () => user.Deactivate("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*La raison de désactivation est requise*");
        }

        [Test]
        public void Reactivate_WhenInactive_ShouldReactivateUser()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            user.Deactivate("Test");

            // Act
            user.Reactivate();

            // Assert
            user.IsActive.Should().BeTrue();
            user.DeactivatedAt.Should().BeNull();
            user.DeactivationReason.Should().BeNull();
        }

        [Test]
        public void Reactivate_WhenActive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            // Act
            Action act = () => user.Reactivate();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Le compte est déjà actif*");
        }

        [Test]
        public void Activate_WhenInactive_ShouldActivateUser()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            user.Deactivate("Test");

            // Act
            user.Activate();

            // Assert
            user.IsActive.Should().BeTrue();
            user.DeactivatedAt.Should().BeNull();
            user.DeactivationReason.Should().BeNull();
        }

        [Test]
        public void Activate_WhenAlreadyActive_ShouldDoNothing()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            // Act
            user.Activate();

            // Assert
            user.IsActive.Should().BeTrue();
        }

        [Test]
        public void IsNewUser_WhenNeverLoggedIn_ShouldReturnTrue()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            // Act & Assert
            user.IsNewUser().Should().BeTrue();
        }

        [Test]
        public void IsNewUser_AfterLogin_ShouldReturnFalse()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            user.RecordLogin();

            // Act & Assert
            user.IsNewUser().Should().BeFalse();
        }

        [Test]
        public void IsInactiveSince_WhenNeverLoggedIn_ShouldReturnFalse()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            // Act & Assert
            user.IsInactiveSince(30).Should().BeFalse();
        }

        [Test]
        public void IsInactiveSince_WhenRecentlyActive_ShouldReturnFalse()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            user.RecordLogin();

            // Act & Assert
            user.IsInactiveSince(30).Should().BeFalse();
        }
    }
}
