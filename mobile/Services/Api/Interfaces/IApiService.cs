using domain.DTOs.Auth;
using domain.DTOs.WeatherForecast;

namespace mobile.Services.Api.Interfaces
{
    /// <summary>
    /// Interface pour les appels API d'authentification
    /// Responsabilité: Gestion de l'authentification et des utilisateurs
    /// </summary>
    public interface IApiAuthService
    {
        /// <summary>
        /// Authentifie un utilisateur avec email/password
        /// </summary>
        Task<AuthResponse?> LoginAsync(LoginRequest request);

        /// <summary>
        /// Enregistre un nouvel utilisateur
        /// </summary>
        Task<bool> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Valide le token JWT actuel
        /// </summary>
        Task<bool> ValidateTokenAsync();

        /// <summary>
        /// Récupère les informations de l'utilisateur connecté
        /// </summary>
        Task<AuthResponse?> GetCurrentUserAsync();

        /// <summary>
        /// Déconnecte l'utilisateur
        /// </summary>
        Task<bool> LogoutAsync();

        /// <summary>
        /// Vérifie si l'API est joignable
        /// Lève ApiUnavailableException si l'API n'est pas accessible (502, timeout, etc.)
        /// Retourne true si l'API est joignable (même si le token est invalide)
        /// </summary>
        Task<bool> CheckApiAvailabilityAsync();
    }

    /// <summary>
    /// Interface pour les appels API des prévisions météo
    /// Responsabilité: CRUD des prévisions météo
    /// </summary>
    public interface IApiWeatherForecastService
    {
        /// <summary>
        /// Récupère toutes les prévisions météo
        /// </summary>
        Task<List<WeatherForecast>> GetForecastsAsync();

        /// <summary>
        /// Récupère une prévision par son ID
        /// </summary>
        Task<WeatherForecast?> GetForecastByIdAsync(int id);

        /// <summary>
        /// Crée une nouvelle prévision
        /// </summary>
        Task<WeatherForecast?> CreateForecastAsync(CreateWeatherForecastRequest request);

        /// <summary>
        /// Met à jour une prévision existante
        /// </summary>
        Task<bool> UpdateForecastAsync(int id, UpdateWeatherForecastRequest request);

        /// <summary>
        /// Supprime une prévision
        /// </summary>
        Task<bool> DeleteForecastAsync(int id);
    }
}
