using domain.DTOs;
using domain.Entities;
using domain.Interfaces;
using domain.Interfaces.Repositories;
using domain.Services;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace tests.Domain.Services
{
    [TestFixture]
    public class UserManagementServiceTests
    {
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IPublisher> _mockPublisher;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private UserManagementService _service;

        [SetUp]
        public void SetUp()
        {
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockPublisher = new Mock<IPublisher>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

            _service = new UserManagementService(
                _mockUserManager.Object,
                _mockUnitOfWork.Object,
                _mockPublisher.Object,
                _mockHttpContextAccessor.Object);
        }

        [Test]
        public async Task RegisterAsync_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var email = "newuser@example.com";
            var password = "Password123";
            var firstName = "John";
            var lastName = "Doe";
            var user = new ApplicationUser(email, firstName, lastName);

            _mockUserManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var (success, errors, returnedUser) = await _service.RegisterAsync(email, password, firstName, lastName);

            // Assert
            success.Should().BeTrue();
            errors.Should().BeEmpty();
            returnedUser.Should().NotBeNull();
            returnedUser!.Email.Should().Be(email);
            returnedUser.FirstName.Should().Be(firstName);
            returnedUser.LastName.Should().Be(lastName);
        }

        [Test]
        public async Task RegisterAsync_WhenCreationFails_ShouldReturnErrors()
        {
            // Arrange
            var email = "invalid@example.com";
            var password = "weak";
            var identityErrors = new[]
            {
                new IdentityError { Description = "Password too weak" },
                new IdentityError { Description = "Email already exists" }
            };

            _mockUserManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), password))
                .ReturnsAsync(IdentityResult.Failed(identityErrors));

            // Act
            var (success, errors, user) = await _service.RegisterAsync(email, password, "John", "Doe");

            // Assert
            success.Should().BeFalse();
            errors.Should().HaveCount(2);
            errors.Should().Contain("Password too weak");
            errors.Should().Contain("Email already exists");
            user.Should().BeNull();
        }

        [Test]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnUser()
        {
            // Arrange
            var userId = "user123";
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.Should().Be(user);
        }

        [Test]
        public async Task GetByEmailAsync_WithExistingEmail_ShouldReturnUser()
        {
            // Arrange
            var email = "test@example.com";
            var user = new ApplicationUser(email, "John", "Doe");

            _mockUserRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);

            // Act
            var result = await _service.GetByEmailAsync(email);

            // Assert
            result.Should().Be(user);
        }



        [Test]
        public async Task UpdateLastLoginAsync_ShouldUpdateLoginDate()
        {
            // Arrange
            var userId = "user123";
            var user = new ApplicationUser("test@example.com", "John", "Doe");

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            await _service.UpdateLastLoginAsync(userId);

            // Assert
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task SearchUsersAsync_WithCriteria_ShouldReturnFilteredUsers()
        {
            // Arrange
            var criteria = new UserSearchCriteria
            {
                SearchTerm = "john",
                IsActive = true,
                PageNumber = 1,
                PageSize = 10
            };

            var users = new List<ApplicationUser>
            {
                new ApplicationUser("john1@example.com", "John", "Doe"),
                new ApplicationUser("john2@example.com", "John", "Smith")
            };

            var pagedResult = new PagedResult<ApplicationUser>
            {
                Items = users,
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };

            _mockUserRepository.Setup(r => r.SearchUsersAsync(criteria)).ReturnsAsync(pagedResult);

            // Act
            var result = await _service.SearchUsersAsync(criteria);

            // Assert
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }
    }
}
