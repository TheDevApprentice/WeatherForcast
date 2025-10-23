using application.Handlers.WeatherForecast;
using application.Hubs;
using domain.Entities;
using domain.Events.WeatherForecast;
using domain.ValueObjects;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace tests.Application.Handlers
{
    [TestFixture]
    public class SignalRForecastNotificationHandlerTests
    {
        private Mock<IHubContext<WeatherForecastHub>> _mockHubContext;
        private Mock<ILogger<SignalRForecastNotificationHandler>> _mockLogger;
        private Mock<IHubClients> _mockClients;
        private Mock<IClientProxy> _mockClientProxy;
        private SignalRForecastNotificationHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _mockHubContext = new Mock<IHubContext<WeatherForecastHub>>();
            _mockLogger = new Mock<ILogger<SignalRForecastNotificationHandler>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

            _handler = new SignalRForecastNotificationHandler(_mockHubContext.Object, _mockLogger.Object);
        }

        [Test]
        public async Task Handle_ForecastCreatedEvent_ShouldBroadcastToAllClients()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(25), "Sunny");
            var notification = new ForecastCreatedEvent(forecast, "TestUser");

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ForecastCreated",
                    It.Is<object[]>(args => args.Length == 1 && args[0] == forecast),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Test]
        public async Task Handle_ForecastUpdatedEvent_ShouldBroadcastToAllClients()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(30), "Hot");
            var notification = new ForecastUpdatedEvent(forecast);

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ForecastUpdated",
                    It.Is<object[]>(args => args.Length == 1 && args[0] == forecast),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Test]
        public async Task Handle_ForecastDeletedEvent_ShouldBroadcastIdToAllClients()
        {
            // Arrange
            var notification = new ForecastDeletedEvent(42);

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ForecastDeleted",
                    It.Is<object[]>(args => args.Length == 1 && (int)args[0] == 42),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Test]
        public async Task Handle_WhenSignalRThrows_ShouldLogErrorAndNotThrow()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(25), "Sunny");
            var notification = new ForecastCreatedEvent(forecast);

            _mockClientProxy
                .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("SignalR error"));

            // Act
            Func<Task> act = async () => await _handler.Handle(notification, CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync();
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Test]
        public async Task Handle_ForecastCreatedEvent_ShouldLogInformation()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(25), "Sunny");
            var notification = new ForecastCreatedEvent(forecast, "TestUser");

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Test]
        public async Task Handle_MultipleConcurrentEvents_ShouldHandleAll()
        {
            // Arrange
            var events = Enumerable.Range(1, 10).Select(i =>
                new ForecastCreatedEvent(
                    new WeatherForecast(DateTime.UtcNow.AddDays(i), new Temperature(20 + i), $"Day {i}")
                )
            ).ToList();

            // Act
            var tasks = events.Select(e => _handler.Handle(e, CancellationToken.None));
            await Task.WhenAll(tasks);

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ForecastCreated",
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Exactly(10)
            );
        }
    }
}
