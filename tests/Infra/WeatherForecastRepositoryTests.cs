using domain.Entities;
using domain.ValueObjects;
using infra.Data;
using infra.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace tests.Infra
{
    [TestFixture]
    public class WeatherForecastRepositoryTests
    {
        private AppDbContext _context;
        private WeatherForecastRepository _repository;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            _repository = new WeatherForecastRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetAllAsync_WhenEmpty_ShouldReturnEmptyList()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetAllAsync_WithForecasts_ShouldReturnAllOrderedByDate()
        {
            // Arrange
            var forecast1 = new WeatherForecast(DateTime.UtcNow.AddDays(2), new Temperature(20), "Later");
            var forecast2 = new WeatherForecast(DateTime.UtcNow.AddDays(1), new Temperature(25), "Earlier");
            var forecast3 = new WeatherForecast(DateTime.UtcNow.AddDays(3), new Temperature(30), "Latest");

            await _repository.AddAsync(forecast1);
            await _repository.AddAsync(forecast2);
            await _repository.AddAsync(forecast3);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _repository.GetAllAsync()).ToList();

            // Assert
            result.Should().HaveCount(3);
            result[0].Summary.Should().Be("Earlier");
            result[1].Summary.Should().Be("Later");
            result[2].Summary.Should().Be("Latest");
        }

        [Test]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnForecast()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Sunny");
            await _repository.AddAsync(forecast);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(forecast.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Summary.Should().Be("Sunny");
            result.TemperatureC.Should().Be(20);
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
        public async Task GetByDateRangeAsync_ShouldReturnForecastsInRange()
        {
            // Arrange
            var baseDate = DateTime.UtcNow;
            var forecast1 = new WeatherForecast(baseDate.AddDays(1), new Temperature(20), "Day 1");
            var forecast2 = new WeatherForecast(baseDate.AddDays(5), new Temperature(25), "Day 5");
            var forecast3 = new WeatherForecast(baseDate.AddDays(10), new Temperature(30), "Day 10");

            await _repository.AddAsync(forecast1);
            await _repository.AddAsync(forecast2);
            await _repository.AddAsync(forecast3);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _repository.GetByDateRangeAsync(
                baseDate.AddDays(2),
                baseDate.AddDays(8)
            )).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].Summary.Should().Be("Day 5");
        }

        [Test]
        public async Task GetByDateRangeAsync_WhenNoForecastsInRange_ShouldReturnEmpty()
        {
            // Arrange
            var baseDate = DateTime.UtcNow;
            var forecast = new WeatherForecast(baseDate.AddDays(1), new Temperature(20), "Day 1");
            await _repository.AddAsync(forecast);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByDateRangeAsync(
                baseDate.AddDays(5),
                baseDate.AddDays(10)
            );

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public async Task AddAsync_ShouldAddForecast()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "New");

            // Act
            await _repository.AddAsync(forecast);
            await _context.SaveChangesAsync();

            // Assert
            var saved = await _context.WeatherForecasts.FirstOrDefaultAsync();
            saved.Should().NotBeNull();
            saved!.Summary.Should().Be("New");
        }

        [Test]
        public async Task Update_ShouldModifyForecast()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Original");
            await _repository.AddAsync(forecast);
            await _context.SaveChangesAsync();

            // Act
            forecast.UpdateSummary("Updated");
            _repository.Update(forecast);
            await _context.SaveChangesAsync();

            // Assert
            var updated = await _context.WeatherForecasts.FindAsync(forecast.Id);
            updated!.Summary.Should().Be("Updated");
        }

        [Test]
        public async Task Delete_ShouldRemoveForecast()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "ToDelete");
            await _repository.AddAsync(forecast);
            await _context.SaveChangesAsync();

            // Act
            _repository.Delete(forecast);
            await _context.SaveChangesAsync();

            // Assert
            var deleted = await _context.WeatherForecasts.FindAsync(forecast.Id);
            deleted.Should().BeNull();
        }

        [Test]
        public async Task AddAsync_MultipleForecasts_ShouldAddAll()
        {
            // Arrange
            var forecast1 = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "First");
            var forecast2 = new WeatherForecast(DateTime.UtcNow.AddDays(1), new Temperature(25), "Second");

            // Act
            await _repository.AddAsync(forecast1);
            await _repository.AddAsync(forecast2);
            await _context.SaveChangesAsync();

            // Assert
            var count = await _context.WeatherForecasts.CountAsync();
            count.Should().Be(2);
        }

        [Test]
        public async Task GetByDateRangeAsync_ShouldOrderByDate()
        {
            // Arrange
            var baseDate = DateTime.UtcNow;
            var forecast1 = new WeatherForecast(baseDate.AddDays(3), new Temperature(30), "Day 3");
            var forecast2 = new WeatherForecast(baseDate.AddDays(1), new Temperature(20), "Day 1");
            var forecast3 = new WeatherForecast(baseDate.AddDays(2), new Temperature(25), "Day 2");

            await _repository.AddAsync(forecast1);
            await _repository.AddAsync(forecast2);
            await _repository.AddAsync(forecast3);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _repository.GetByDateRangeAsync(
                baseDate,
                baseDate.AddDays(5)
            )).ToList();

            // Assert
            result.Should().HaveCount(3);
            result[0].Summary.Should().Be("Day 1");
            result[1].Summary.Should().Be("Day 2");
            result[2].Summary.Should().Be("Day 3");
        }
    }
}
