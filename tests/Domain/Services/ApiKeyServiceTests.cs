using domain.Entities;
using domain.Interfaces;
using domain.Interfaces.Repositories;
using domain.Services;
using domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace tests.Domain.Services
{
    [TestFixture]
    public class ApiKeyServiceTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IApiKeyRepository> _mockRepository;
        private ApiKeyService _service;

        [SetUp]
        public void SetUp()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRepository = new Mock<IApiKeyRepository>();

            _mockUnitOfWork.Setup(u => u.ApiKeys).Returns(_mockRepository.Object);

            _service = new ApiKeyService(_mockUnitOfWork.Object);
        }

        [Test]
        public async Task ValidateApiKeyAsync_WithInvalidKey_ShouldReturnFalse()
        {
            // Arrange
            var key = "wf_live_invalid";
            var secret = "wf_secret_test";

            _mockRepository.Setup(r => r.GetByKeyAsync(key)).ReturnsAsync((ApiKey?)null);

            // Act
            var (isValid, returnedKey) = await _service.ValidateApiKeyAsync(key, secret);

            // Assert
            isValid.Should().BeFalse();
            returnedKey.Should().BeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
        }

        [Test]
        public async Task GetByUserIdAsync_ShouldReturnAllUserKeys()
        {
            // Arrange
            var userId = "user123";
            var apiKeys = new List<ApiKey>
            {
                new ApiKey("Key 1", "wf_live_1", "hash1", userId, ApiKeyScopes.ReadOnly),
                new ApiKey("Key 2", "wf_live_2", "hash2", userId, ApiKeyScopes.ReadWrite)
            };

            _mockRepository.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(apiKeys);

            // Act
            var result = await _service.GetUserApiKeysAsync(userId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(apiKeys);
            _mockRepository.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Test]
        public async Task RevokeAsync_WithExistingKey_ShouldRevokeAndSave()
        {
            // Arrange
            var apiKey = new ApiKey("Test Key", "wf_live_test", "hash", "user123", ApiKeyScopes.ReadOnly);
            var reason = "Security breach";

            _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(apiKey);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _service.RevokeApiKeyAsync(apiKey.Id, "user123", reason);

            // Assert
            result.Should().BeTrue();
            apiKey.IsActive.Should().BeFalse();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task RevokeAsync_WithNonExistingKey_ShouldReturnFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ApiKey?)null);

            // Act
            var result = await _service.RevokeApiKeyAsync(999, "", "Reason");

            // Assert
            result.Should().BeFalse();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
        }
    }
}
