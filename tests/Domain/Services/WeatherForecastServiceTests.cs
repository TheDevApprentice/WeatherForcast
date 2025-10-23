using domain.Entities;
using domain.Events.WeatherForecast;
using domain.Interfaces;
using domain.Interfaces.Repositories;
using domain.Services;
using domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Moq;
using NUnit.Framework;

namespace tests.Domain.Services
{
    [TestFixture]
    public class WeatherForecastServiceTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IPublisher> _mockPublisher;
        private Mock<IWeatherForecastRepository> _mockRepository;
        private Mock<ISignalRConnectionService> _mockConnectionService;
        private WeatherForecastService _service;

        [SetUp]
        public void SetUp()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockPublisher = new Mock<IPublisher>();
            _mockRepository = new Mock<IWeatherForecastRepository>();
            _mockConnectionService = new Mock<ISignalRConnectionService>();

            // Setup UnitOfWork to return the mock repository
            _mockUnitOfWork.Setup(u => u.WeatherForecasts).Returns(_mockRepository.Object);
            
            // Setup ConnectionService to return null by default (no ConnectionId in tests)
            _mockConnectionService.Setup(c => c.GetCurrentConnectionId()).Returns((string?)null);

            _service = new WeatherForecastService(
                _mockUnitOfWork.Object, 
                _mockPublisher.Object,
                _mockConnectionService.Object);
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnAllForecasts()
        {
            // Arrange
            var forecasts = new List<WeatherForecast>
            {
                new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Sunny"),
                new WeatherForecast(DateTime.UtcNow.AddDays(1), new Temperature(25), "Cloudy")
            };
            _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(forecasts);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(forecasts);
            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Test]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnForecast()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Sunny");
            _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(forecast);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(forecast);
            _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        [Test]
        public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((WeatherForecast?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
            _mockRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
        }

        [Test]
        public async Task CreateAsync_ShouldAddForecastAndPublishEvent()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Sunny");
            _mockRepository.Setup(r => r.AddAsync(forecast)).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(forecast);

            // Assert
            result.Should().Be(forecast);
            _mockRepository.Verify(r => r.AddAsync(forecast), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
            _mockPublisher.Verify(p => p.Publish(
                It.Is<ForecastCreatedEvent>(e => e.Forecast == forecast),
                default), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_WithExistingForecast_ShouldUpdateAndPublishEvent()
        {
            // Arrange
            var existingForecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Sunny");
            var newDate = DateTime.UtcNow.AddDays(1);
            var newTemperature = new Temperature(25);
            var newSummary = "Cloudy";

            _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingForecast);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _service.UpdateAsync(1, newDate, newTemperature, newSummary);

            // Assert
            result.Should().BeTrue();
            existingForecast.Date.Should().Be(newDate);
            existingForecast.Temperature.Should().Be(newTemperature);
            existingForecast.Summary.Should().Be(newSummary);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
            _mockPublisher.Verify(p => p.Publish(
                It.Is<ForecastUpdatedEvent>(e => e.Forecast == existingForecast),
                default), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_WithNonExistingForecast_ShouldReturnFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((WeatherForecast?)null);
            var newDate = DateTime.UtcNow;
            var newTemperature = new Temperature(25);

            // Act
            var result = await _service.UpdateAsync(999, newDate, newTemperature, "Test");

            // Assert
            result.Should().BeFalse();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
            _mockPublisher.Verify(p => p.Publish(It.IsAny<ForecastUpdatedEvent>(), default), Times.Never);
        }

        [Test]
        public async Task DeleteAsync_WithExistingForecast_ShouldDeleteAndPublishEvent()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Sunny");
            _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(forecast);
            _mockRepository.Setup(r => r.Delete(forecast));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(r => r.Delete(forecast), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
            _mockPublisher.Verify(p => p.Publish(
                It.Is<ForecastDeletedEvent>(e => e.Id == 1),
                default), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_WithNonExistingForecast_ShouldReturnFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((WeatherForecast?)null);

            // Act
            var result = await _service.DeleteAsync(999);

            // Assert
            result.Should().BeFalse();
            _mockRepository.Verify(r => r.Delete(It.IsAny<WeatherForecast>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Never);
            _mockPublisher.Verify(p => p.Publish(It.IsAny<ForecastDeletedEvent>(), default), Times.Never);
        }
    }
}
