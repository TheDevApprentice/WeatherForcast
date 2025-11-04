using mobile.Models;
using mobile.Models.DTOs;

namespace mobile.Services
{
    /// <summary>
    /// Service combiné pour compatibilité ascendante
    /// Délègue aux services spécialisés ApiAuthService et ApiWeatherForecastService
    /// </summary>
    [Obsolete("Utilisez IApiAuthService et IApiWeatherForecastService à la place")]
    public class ApiService : IApiService
    {
        private readonly IApiAuthService _authService;
        private readonly IApiWeatherForecastService _forecastService;

        public ApiService(IApiAuthService authService, IApiWeatherForecastService forecastService)
        {
            _authService = authService;
            _forecastService = forecastService;
        }

        #region Authentification - Délégation à ApiAuthService

        public Task<AuthResponse?> LoginAsync(LoginRequest request)
            => _authService.LoginAsync(request);

        public Task<bool> RegisterAsync(RegisterRequest request)
            => _authService.RegisterAsync(request);

        public Task<bool> ValidateTokenAsync()
            => _authService.ValidateTokenAsync();

        public Task<CurrentUserResponse?> GetCurrentUserAsync()
            => _authService.GetCurrentUserAsync();

        public Task<bool> LogoutAsync()
            => _authService.LogoutAsync();

        #endregion

        #region WeatherForecast - Délégation à ApiWeatherForecastService

        public Task<List<WeatherForecast>> GetForecastsAsync()
            => _forecastService.GetForecastsAsync();

        public Task<WeatherForecast?> GetForecastByIdAsync(int id)
            => _forecastService.GetForecastByIdAsync(id);

        public Task<WeatherForecast?> CreateForecastAsync(CreateForecastRequest request)
            => _forecastService.CreateForecastAsync(request);

        public Task<bool> UpdateForecastAsync(int id, UpdateForecastRequest request)
            => _forecastService.UpdateForecastAsync(id, request);

        public Task<bool> DeleteForecastAsync(int id)
            => _forecastService.DeleteForecastAsync(id);

        #endregion
    }
}
