using domain.Entities;
using domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace tests.Domain.Entities
{
    [TestFixture]
    public class WeatherForecastTests
    {
        [Test]
        public void Constructor_WithValidParameters_ShouldCreateForecast()
        {
            // Arrange
            var date = DateTime.UtcNow.AddDays(1);
            var temperature = new Temperature(25);
            var summary = "Sunny";

            // Act
            var forecast = new WeatherForecast(date, temperature, summary);

            // Assert
            forecast.Date.Should().Be(date);
            forecast.Temperature.Should().Be(temperature);
            forecast.Summary.Should().Be(summary);
            forecast.TemperatureC.Should().Be(25);
            forecast.TemperatureF.Should().Be(77);
        }

        [Test]
        public void Constructor_WithNullTemperature_ShouldThrowArgumentNullException()
        {
            // Arrange
            var date = DateTime.UtcNow.AddDays(1);

            // Act
            Action act = () => new WeatherForecast(date, null!, "Sunny");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Constructor_WithDateMoreThan1YearInPast_ShouldThrowArgumentException()
        {
            // Arrange
            var oldDate = DateTime.UtcNow.AddYears(-2);
            var temperature = new Temperature(25);

            // Act
            Action act = () => new WeatherForecast(oldDate, temperature, "Sunny");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*La date ne peut pas être antérieure à 1 an*");
        }

        [Test]
        public void Constructor_WithDateMoreThan1YearInFuture_ShouldThrowArgumentException()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddYears(2);
            var temperature = new Temperature(25);

            // Act
            Action act = () => new WeatherForecast(futureDate, temperature, "Sunny");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*La date ne peut pas être supérieure à 1 an dans le futur*");
        }

        [Test]
        [TestCase(-365)]
        [TestCase(0)]
        [TestCase(180)]
        [TestCase(365)]
        public void Constructor_WithValidDateRange_ShouldNotThrow(int daysOffset)
        {
            // Arrange
            var date = DateTime.UtcNow.AddDays(daysOffset);
            var temperature = new Temperature(20);

            // Act
            Action act = () => new WeatherForecast(date, temperature, "Valid");

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void UpdateTemperature_WithValidTemperature_ShouldUpdateValue()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Mild");
            var newTemperature = new Temperature(30);

            // Act
            forecast.UpdateTemperature(newTemperature);

            // Assert
            forecast.Temperature.Should().Be(newTemperature);
            forecast.TemperatureC.Should().Be(30);
        }

        [Test]
        public void UpdateTemperature_WithNullTemperature_ShouldThrowArgumentNullException()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Mild");

            // Act
            Action act = () => forecast.UpdateTemperature(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void UpdateSummary_WithNewSummary_ShouldUpdateValue()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Sunny");
            var newSummary = "Cloudy";

            // Act
            forecast.UpdateSummary(newSummary);

            // Assert
            forecast.Summary.Should().Be(newSummary);
        }

        [Test]
        public void UpdateSummary_WithNull_ShouldAcceptNull()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Sunny");

            // Act
            forecast.UpdateSummary(null);

            // Assert
            forecast.Summary.Should().BeNull();
        }

        [Test]
        public void UpdateDate_WithValidDate_ShouldUpdateValue()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Sunny");
            var newDate = DateTime.UtcNow.AddDays(5);

            // Act
            forecast.UpdateDate(newDate);

            // Assert
            forecast.Date.Should().Be(newDate);
        }

        [Test]
        public void UpdateDate_WithInvalidDate_ShouldThrowArgumentException()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(20), "Sunny");
            var invalidDate = DateTime.UtcNow.AddYears(-2);

            // Act
            Action act = () => forecast.UpdateDate(invalidDate);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void TemperatureC_ShouldReturnCelsiusValue()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(25), "Warm");

            // Act & Assert
            forecast.TemperatureC.Should().Be(25);
        }

        [Test]
        public void TemperatureF_ShouldReturnFahrenheitValue()
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(0), "Cold");

            // Act & Assert
            forecast.TemperatureF.Should().Be(32);
        }

        [Test]
        [TestCase(31, true)]
        [TestCase(35, true)]
        [TestCase(40, true)]
        [TestCase(30, false)]
        [TestCase(20, false)]
        [TestCase(0, false)]
        public void IsHot_ShouldReturnCorrectValue(int celsius, bool expectedIsHot)
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(celsius), "Test");

            // Act & Assert
            forecast.IsHot().Should().Be(expectedIsHot);
        }

        [Test]
        [TestCase(-1, true)]
        [TestCase(-10, true)]
        [TestCase(-20, true)]
        [TestCase(0, false)]
        [TestCase(10, false)]
        [TestCase(20, false)]
        public void IsCold_ShouldReturnCorrectValue(int celsius, bool expectedIsCold)
        {
            // Arrange
            var forecast = new WeatherForecast(DateTime.UtcNow, new Temperature(celsius), "Test");

            // Act & Assert
            forecast.IsCold().Should().Be(expectedIsCold);
        }

        [Test]
        public void Constructor_WithNullSummary_ShouldAcceptNull()
        {
            // Arrange
            var date = DateTime.UtcNow;
            var temperature = new Temperature(20);

            // Act
            var forecast = new WeatherForecast(date, temperature, null);

            // Assert
            forecast.Summary.Should().BeNull();
        }
    }
}
