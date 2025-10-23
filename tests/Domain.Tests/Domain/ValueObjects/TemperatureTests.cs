using domain.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace tests.Domain.ValueObjects
{
    [TestFixture]
    public class TemperatureTests
    {
        [Test]
        public void Constructor_WithValidCelsius_ShouldCreateTemperature()
        {
            // Arrange
            int celsius = 25;

            // Act
            var temperature = new Temperature(celsius);

            // Assert
            temperature.Celsius.Should().Be(25);
            temperature.Fahrenheit.Should().Be(76); // Arrondi du calcul réel
        }

        [Test]
        [TestCase(-100)]
        [TestCase(0)]
        [TestCase(50)]
        [TestCase(100)]
        public void Constructor_WithValidRange_ShouldNotThrow(int celsius)
        {
            // Act
            Action act = () => new Temperature(celsius);

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        [TestCase(-101)]
        [TestCase(101)]
        [TestCase(-200)]
        [TestCase(200)]
        public void Constructor_WithInvalidRange_ShouldThrowArgumentException(int celsius)
        {
            // Act
            Action act = () => new Temperature(celsius);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage($"*Température invalide*{celsius}*");
        }

        [Test]
        public void Fahrenheit_ShouldBeCalculatedCorrectly()
        {
            // Arrange & Act
            var temp0 = new Temperature(0);
            var temp100 = new Temperature(100);
            var tempMinus40 = new Temperature(-40);

            // Assert
            temp0.Fahrenheit.Should().Be(32);
            temp100.Fahrenheit.Should().Be(212);
            tempMinus40.Fahrenheit.Should().Be(-40); // -40°C = -40°F
        }

        [Test]
        [TestCase(31, true)]
        [TestCase(40, true)]
        [TestCase(100, true)]
        [TestCase(30, false)]
        [TestCase(0, false)]
        [TestCase(-10, false)]
        public void IsHot_ShouldReturnCorrectValue(int celsius, bool expectedIsHot)
        {
            // Arrange
            var temperature = new Temperature(celsius);

            // Act & Assert
            temperature.IsHot.Should().Be(expectedIsHot);
        }

        [Test]
        [TestCase(-1, true)]
        [TestCase(-10, true)]
        [TestCase(-100, true)]
        [TestCase(0, false)]
        [TestCase(10, false)]
        [TestCase(100, false)]
        public void IsCold_ShouldReturnCorrectValue(int celsius, bool expectedIsCold)
        {
            // Arrange
            var temperature = new Temperature(celsius);

            // Act & Assert
            temperature.IsCold.Should().Be(expectedIsCold);
        }

        [Test]
        public void Equality_WithSameValue_ShouldBeEqual()
        {
            // Arrange
            var temp1 = new Temperature(25);
            var temp2 = new Temperature(25);

            // Act & Assert
            temp1.Should().Be(temp2);
            (temp1 == temp2).Should().BeTrue();
        }

        [Test]
        public void Equality_WithDifferentValue_ShouldNotBeEqual()
        {
            // Arrange
            var temp1 = new Temperature(25);
            var temp2 = new Temperature(26);

            // Act & Assert
            temp1.Should().NotBe(temp2);
            (temp1 != temp2).Should().BeTrue();
        }

        [Test]
        public void ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var temperature = new Temperature(25);

            // Act
            var result = temperature.ToString();

            // Assert
            result.Should().Contain("25°C");
            result.Should().Contain("77°F");
        }

        [Test]
        public void FromFahrenheit_ShouldConvertCorrectly()
        {
            // Arrange & Act
            var temp1 = Temperature.FromFahrenheit(32);  // 0°C
            var temp2 = Temperature.FromFahrenheit(212); // 100°C
            var temp3 = Temperature.FromFahrenheit(-40); // -40°C

            // Assert
            temp1.Celsius.Should().Be(0);
            temp2.Celsius.Should().Be(100);
            temp3.Celsius.Should().Be(-40);
        }
    }
}
