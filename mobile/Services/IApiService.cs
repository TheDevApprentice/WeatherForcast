using mobile.Models;
using mobile.Models.DTOs;

namespace mobile.Services
{
    /// <summary>
    /// Interface pour les appels API REST
    /// </summary>
    public interface IApiService
    {
        // Authentification
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<bool> RegisterAsync(RegisterRequest request);
        Task<bool> ValidateTokenAsync();
        Task<bool> LogoutAsync();

        // WeatherForecast
        Task<List<WeatherForecast>> GetForecastsAsync();
        Task<WeatherForecast?> GetForecastByIdAsync(int id);
        Task<WeatherForecast?> CreateForecastAsync(CreateForecastRequest request);
        Task<bool> UpdateForecastAsync(int id, UpdateForecastRequest request);
        Task<bool> DeleteForecastAsync(int id);
    }
}
