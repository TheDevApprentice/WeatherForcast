using domain.Entities;
using domain.Interfaces;
using domain.Interfaces.Repositories;
using domain.Services;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace tests.Domain.Services
{
    [TestFixture]
    public class SessionManagementServiceTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ISessionRepository> _mockSessionRepository;
        private Mock<IPublisher> _mockPublisher;
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private SessionManagementService _service;

        [SetUp]
        public void SetUp()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockSessionRepository = new Mock<ISessionRepository>();
            _mockPublisher = new Mock<IPublisher>();
            
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _mockUnitOfWork.Setup(u => u.Sessions).Returns(_mockSessionRepository.Object);

            _service = new SessionManagementService(
                _mockUnitOfWork.Object,
                _mockPublisher.Object,
                _mockUserManager.Object);
        }

        [Test]
        public async Task GetActiveSessionsAsync_ShouldReturnOnlyActiveSessions()
        {
            // Arrange
            var userId = "user123";
            var activeSessions = new List<Session>
            {
                new Session("token1", SessionType.Web, DateTime.UtcNow.AddDays(7)),
                new Session("token2", SessionType.Web, DateTime.UtcNow.AddDays(7))
            };

            _mockSessionRepository.Setup(r => r.GetActiveSessionsByUserIdAsync(userId)).ReturnsAsync(activeSessions);

            // Act
            var result = await _service.GetActiveSessionsAsync(userId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(activeSessions);
        }

        [Test]
        public async Task RevokeAllByUserIdAsync_ShouldRevokeAllUserSessions()
        {
            // Arrange
            var userId = "user123";
            var sessions = new List<Session>
            {
                new Session("token1", SessionType.Web, DateTime.UtcNow.AddDays(7)),
                new Session("token2", SessionType.Web, DateTime.UtcNow.AddDays(7)),
                new Session("token3", SessionType.Api, DateTime.UtcNow.AddHours(24))
            };

            _mockSessionRepository.Setup(r => r.GetActiveSessionsByUserIdAsync(userId)).ReturnsAsync(sessions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            await _service.RevokeAllByUserIdAsync(userId);

            // Assert
            sessions.Should().AllSatisfy(s => s.IsValid().Should().BeFalse());
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }
    }
}
