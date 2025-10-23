using domain.Entities;
using domain.Events;
using domain.Interfaces.Services;
using domain.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace tests.Domain.Services
{
    [TestFixture]
    public class AuthenticationServiceTests
    {
        private Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private Mock<IUserManagementService> _mockUserManagementService;
        private Mock<ISessionManagementService> _mockSessionManagementService;
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<IPublisher> _mockPublisher;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private AuthenticationService _service;

        [SetUp]
        public void SetUp()
        {
            // Mock UserManager (requis pour SignInManager)
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Mock SignInManager
            var contextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                contextAccessorMock.Object,
                claimsFactoryMock.Object,
                null, null, null, null);

            _mockUserManagementService = new Mock<IUserManagementService>();
            _mockSessionManagementService = new Mock<ISessionManagementService>();
            _mockPublisher = new Mock<IPublisher>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _service = new AuthenticationService(
                _mockSignInManager.Object,
                _mockUserManagementService.Object,
                _mockSessionManagementService.Object,
                _mockPublisher.Object,
                _mockHttpContextAccessor.Object
                );
        }

        [Test]
        public async Task ValidateCredentialsAsync_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Password123";
            var user = new ApplicationUser(email, "John", "Doe");

            _mockUserManagementService
                .Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync(user);

            _mockSignInManager
                .Setup(s => s.CheckPasswordSignInAsync(user, password, true))
                .ReturnsAsync(SignInResult.Success);

            // Act
            var (success, returnedUser) = await _service.ValidateCredentialsAsync(email, password);

            // Assert
            success.Should().BeTrue();
            returnedUser.Should().Be(user);
            _mockUserManagementService.Verify(s => s.GetByEmailAsync(email), Times.Once);
            _mockSignInManager.Verify(s => s.CheckPasswordSignInAsync(user, password, true), Times.Once);
        }

        [Test]
        public async Task ValidateCredentialsAsync_WithInvalidEmail_ShouldReturnFailure()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var password = "Password123";

            _mockUserManagementService
                .Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var (success, returnedUser) = await _service.ValidateCredentialsAsync(email, password);

            // Assert
            success.Should().BeFalse();
            returnedUser.Should().BeNull();
            _mockSignInManager.Verify(s => s.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task ValidateCredentialsAsync_WithInvalidPassword_ShouldReturnFailure()
        {
            // Arrange
            var email = "test@example.com";
            var password = "WrongPassword";
            var user = new ApplicationUser(email, "John", "Doe");

            _mockUserManagementService
                .Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync(user);

            _mockSignInManager
                .Setup(s => s.CheckPasswordSignInAsync(user, password, true))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var (success, returnedUser) = await _service.ValidateCredentialsAsync(email, password);

            // Assert
            success.Should().BeFalse();
            returnedUser.Should().BeNull();
        }

        [Test]
        public async Task RegisterWithSessionAsync_WhenUserCreationFails_ShouldReturnErrors()
        {
            // Arrange
            var email = "newuser@example.com";
            var password = "weak";
            var errors = new[] { "Password too weak" };

            _mockUserManagementService
                .Setup(s => s.RegisterAsync(email, password, "John", "Doe"))
                .ReturnsAsync((false, errors, null));

            // Act
            var (success, returnedErrors, user) = await _service.RegisterWithSessionAsync(
                email, password, "John", "Doe", "token");

            // Assert
            success.Should().BeFalse();
            returnedErrors.Should().Contain("Password too weak");
            user.Should().BeNull();
            _mockSessionManagementService.Verify(s => s.CreateWebSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task LoginWithSessionAsync_WithInvalidCredentials_ShouldNotCreateSession()
        {
            // Arrange
            var email = "test@example.com";
            var password = "WrongPassword";
            var sessionToken = "newsession";
            var user = new ApplicationUser(email, "John", "Doe");

            _mockUserManagementService
                .Setup(s => s.GetByEmailAsync(email))
                .ReturnsAsync(user);

            _mockSignInManager
                .Setup(s => s.CheckPasswordSignInAsync(user, password, true))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var (success, returnedUser) = await _service.LoginWithSessionAsync(
                email, password, sessionToken);

            // Assert
            success.Should().BeFalse();
            returnedUser.Should().BeNull();
            _mockSessionManagementService.Verify(s => s.RevokeAllByUserIdAsync(It.IsAny<string>()), Times.Never);
            _mockSessionManagementService.Verify(s => s.CreateWebSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }
    }
}
