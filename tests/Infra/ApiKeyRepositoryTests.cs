using domain.Entities;
using domain.ValueObjects;
using FluentAssertions;
using infra.Data;
using infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace tests.Infra
{
    [TestFixture]
    public class ApiKeyRepositoryTests
    {
        private AppDbContext _context;
        private ApiKeyRepository _repository;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _repository = new ApiKeyRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetByKeyAsync_WithExistingKey_ShouldReturnApiKey()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var apiKey = new ApiKey("Test Key", "wf_live_test123", "secretHash", user.Id, ApiKeyScopes.ReadOnly);
            await _repository.CreateAsync(apiKey);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByKeyAsync("wf_live_test123");

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Test Key");
            result.User.Should().NotBeNull();
            result.User!.Email.Should().Be("test@example.com");
        }

        [Test]
        public async Task GetByKeyAsync_WithNonExistingKey_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByKeyAsync("wf_live_nonexistent");

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetByUserIdAsync_ShouldReturnAllUserKeys()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var key1 = new ApiKey("Key 1", "wf_live_1", "hash1", user.Id, ApiKeyScopes.ReadOnly);
            var key2 = new ApiKey("Key 2", "wf_live_2", "hash2", user.Id, ApiKeyScopes.ReadWrite);

            await _repository.CreateAsync(key1);
            await _repository.CreateAsync(key2);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _repository.GetByUserIdAsync(user.Id)).ToList();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(k => k.Name == "Key 1");
            result.Should().Contain(k => k.Name == "Key 2");
        }

        [Test]
        public async Task GetByUserIdAsync_WithNonExistingUser_ShouldReturnEmpty()
        {
            // Act
            var result = await _repository.GetByUserIdAsync("nonexistent");

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnApiKey()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var apiKey = new ApiKey("Test Key", "wf_live_test", "hash", user.Id, ApiKeyScopes.ReadOnly);
            await _repository.CreateAsync(apiKey);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(apiKey.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Test Key");
        }

        [Test]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task AddAsync_ShouldAddApiKey()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var apiKey = new ApiKey("New Key", "wf_live_new", "hash", user.Id, ApiKeyScopes.ReadOnly);

            // Act
            await _repository.CreateAsync(apiKey);
            await _context.SaveChangesAsync();

            // Assert
            var saved = await _context.ApiKeys.FirstOrDefaultAsync();
            saved.Should().NotBeNull();
            saved!.Name.Should().Be("New Key");
        }

        [Test]
        public async Task Update_ShouldModifyApiKey()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var apiKey = new ApiKey("Original", "wf_live_test", "hash", user.Id, ApiKeyScopes.ReadOnly);
            await _repository.CreateAsync(apiKey);
            await _context.SaveChangesAsync();

            // Act
            apiKey.UpdateName("Updated");
            _repository.UpdateAsync(apiKey);
            await _context.SaveChangesAsync();

            // Assert
            var updated = await _context.ApiKeys.FindAsync(apiKey.Id);
            updated!.Name.Should().Be("Updated");
        }

        [Test]
        public async Task Delete_ShouldRemoveApiKey()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var apiKey = new ApiKey("ToDelete", "wf_live_delete", "hash", user.Id, ApiKeyScopes.ReadOnly);
            await _repository.CreateAsync(apiKey);
            await _context.SaveChangesAsync();

            // Act
            apiKey.Revoke("test");
            await _context.SaveChangesAsync();

            // Assert
            var deleted = await _context.ApiKeys.FindAsync(apiKey.Id);
            deleted.Should().NotBeNull();
            deleted.IsRevoked.Should().BeTrue();
        }

        [Test]
        public async Task GetByKeyAsync_ShouldIncludeUserNavigationProperty()
        {
            // Arrange
            var user = new ApplicationUser("test@example.com", "John", "Doe");
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var apiKey = new ApiKey("Test Key", "wf_live_test", "hash", user.Id, ApiKeyScopes.ReadOnly);
            await _repository.CreateAsync(apiKey);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByKeyAsync("wf_live_test");

            // Assert
            result!.User.Should().NotBeNull();
            result.User!.FirstName.Should().Be("John");
            result.User.LastName.Should().Be("Doe");
        }
    }
}
