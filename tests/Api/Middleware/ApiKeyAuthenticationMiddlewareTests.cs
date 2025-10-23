using api.Middleware;
using domain.Constants;
using domain.Entities;
using domain.Interfaces.Services;
using domain.ValueObjects;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Text;

namespace tests.Api.Middleware
{
    [TestFixture]
    public class ApiKeyAuthenticationMiddlewareTests
    {
        private Mock<RequestDelegate> _mockNext;
        private Mock<ILogger<ApiKeyAuthenticationMiddleware>> _mockLogger;
        private Mock<IApiKeyService> _mockApiKeyService;
        private ApiKeyAuthenticationMiddleware _middleware;
        private DefaultHttpContext _httpContext;

        [SetUp]
        public void SetUp()
        {
            _mockNext = new Mock<RequestDelegate>();
            _mockLogger = new Mock<ILogger<ApiKeyAuthenticationMiddleware>>();
            _mockApiKeyService = new Mock<IApiKeyService>();

            _middleware = new ApiKeyAuthenticationMiddleware(_mockNext.Object, _mockLogger.Object);
            _httpContext = new DefaultHttpContext();
        }

        [Test]
        public async Task InvokeAsync_WithSwaggerPath_ShouldSkipAuthentication()
        {
            // Arrange
            _httpContext.Request.Path = "/swagger/index.html";

            // Act
            await _middleware.InvokeAsync(_httpContext, _mockApiKeyService.Object);

            // Assert
            _mockNext.Verify(n => n(_httpContext), Times.Once);
            _mockApiKeyService.Verify(s => s.ValidateApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task InvokeAsync_WithHealthPath_ShouldSkipAuthentication()
        {
            // Arrange
            _httpContext.Request.Path = "/health";

            // Act
            await _middleware.InvokeAsync(_httpContext, _mockApiKeyService.Object);

            // Assert
            _mockNext.Verify(n => n(_httpContext), Times.Once);
        }

        [Test]
        public async Task InvokeAsync_WithoutAuthorizationHeader_ShouldReturn401()
        {
            // Arrange
            _httpContext.Request.Path = "/api/weatherforecast";

            // Act
            await _middleware.InvokeAsync(_httpContext, _mockApiKeyService.Object);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(401);
            _mockNext.Verify(n => n(_httpContext), Times.Never);
        }

        [Test]
        public async Task InvokeAsync_WithInvalidAuthorizationFormat_ShouldReturn401()
        {
            // Arrange
            _httpContext.Request.Path = "/api/weatherforecast";
            _httpContext.Request.Headers["Authorization"] = "Bearer token123";

            // Act
            await _middleware.InvokeAsync(_httpContext, _mockApiKeyService.Object);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(401);
        }

        [Test]
        public async Task InvokeAsync_WithValidApiKey_ShouldAuthenticateAndCallNext()
        {
            // Arrange
            _httpContext.Request.Path = "/api/weatherforecast";
            var key = "wf_live_test123";
            var secret = "wf_secret_test456";
            var credentials = $"{key}:{secret}";
            var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            _httpContext.Request.Headers["Authorization"] = $"Basic {base64Credentials}";

            var user = new ApplicationUser("test@example.com", "John", "Doe");
            var apiKey = new ApiKey("Test Key", key, "hash", user.Id, ApiKeyScopes.ReadWrite);

            _mockApiKeyService
                .Setup(s => s.ValidateApiKeyAsync(key, secret))
                .ReturnsAsync((true, apiKey));

            // Act
            await _middleware.InvokeAsync(_httpContext, _mockApiKeyService.Object);

            // Assert
            _httpContext.Response.StatusCode.Should().NotBe(401);
            _httpContext.User.Identity!.IsAuthenticated.Should().BeTrue();
            _httpContext.User.FindFirst(AppClaims.Permission)?.Value.Should().NotBeNullOrEmpty();
            _mockNext.Verify(n => n(_httpContext), Times.Once);
        }

        [Test]
        public async Task InvokeAsync_WithInvalidApiKey_ShouldReturn401()
        {
            // Arrange
            _httpContext.Request.Path = "/api/weatherforecast";
            var credentials = "wf_live_invalid:wf_secret_invalid";
            var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            _httpContext.Request.Headers["Authorization"] = $"Basic {base64Credentials}";

            _mockApiKeyService
                .Setup(s => s.ValidateApiKeyAsync("wf_live_invalid", "wf_secret_invalid"))
                .ReturnsAsync((false, null));

            // Act
            await _middleware.InvokeAsync(_httpContext, _mockApiKeyService.Object);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(401);
            _mockNext.Verify(n => n(_httpContext), Times.Never);
        }

        [Test]
        public async Task InvokeAsync_WithMalformedBase64_ShouldReturn401()
        {
            // Arrange
            _httpContext.Request.Path = "/api/weatherforecast";
            _httpContext.Request.Headers["Authorization"] = "Basic invalid-base64!!!";

            // Act
            await _middleware.InvokeAsync(_httpContext, _mockApiKeyService.Object);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(401);
        }

        [Test]
        public async Task InvokeAsync_WithValidKey_ShouldAddUserClaimsToContext()
        {
            // Arrange
            _httpContext.Request.Path = "/api/weatherforecast";
            var key = "wf_live_test";
            var secret = "wf_secret_test";
            var credentials = $"{key}:{secret}";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            _httpContext.Request.Headers["Authorization"] = $"Basic {base64}";

            var user = new ApplicationUser("test@example.com", "John", "Doe");
            var apiKey = new ApiKey("Test", key, "hash", user.Id, ApiKeyScopes.FullAccess);
            typeof(ApiKey).GetProperty("User")!.SetValue(apiKey, user);

            _mockApiKeyService
                .Setup(s => s.ValidateApiKeyAsync(key, secret))
                .ReturnsAsync((true, apiKey));

            // Act
            await _middleware.InvokeAsync(_httpContext, _mockApiKeyService.Object);

            // Assert
            _httpContext.User.Identity!.IsAuthenticated.Should().BeTrue();
            _httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value.Should().Be("test@example.com");
            _httpContext.User.FindFirst(AppClaims.AccessType)?.Value.Should().Be(AppClaims.ApiKeyAccess);
            
            // Vérifier que tous les scopes sont ajoutés comme claims
            var permissionClaims = _httpContext.User.FindAll(AppClaims.Permission).Select(c => c.Value).ToList();
            permissionClaims.Should().Contain(AppClaims.ForecastRead);
            permissionClaims.Should().Contain(AppClaims.ForecastWrite);
            permissionClaims.Should().Contain(AppClaims.ForecastDelete);
        }

        [Test]
        public async Task InvokeAsync_WithInvalidCredentialsFormat_ShouldReturn401()
        {
            // Arrange
            _httpContext.Request.Path = "/api/weatherforecast";
            var invalidCredentials = "onlyonepart"; // Pas de ':'
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(invalidCredentials));
            _httpContext.Request.Headers["Authorization"] = $"Basic {base64}";

            // Act
            await _middleware.InvokeAsync(_httpContext, _mockApiKeyService.Object);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(401);
            _mockNext.Verify(n => n(_httpContext), Times.Never);
        }

        [Test]
        public async Task InvokeAsync_WhenServiceThrows_ShouldReturn401()
        {
            // Arrange
            _httpContext.Request.Path = "/api/weatherforecast";
            var credentials = "key:secret";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            _httpContext.Request.Headers["Authorization"] = $"Basic {base64}";

            _mockApiKeyService
                .Setup(s => s.ValidateApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            await _middleware.InvokeAsync(_httpContext, _mockApiKeyService.Object);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(401);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }
    }
}
