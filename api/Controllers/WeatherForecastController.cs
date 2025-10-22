using domain.Entities;
using domain.Interfaces.Services;
using domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    /// <summary>
    /// API Controller WeatherForecast avec Clean Architecture
    /// Utilise IWeatherForecastService (pas de dépendance directe à Infrastructure)
    /// Nécessite une authentification JWT
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IWeatherForecastService _weatherForecastService;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(
            IWeatherForecastService weatherForecastService,
            ILogger<WeatherForecastController> logger)
        {
            _weatherForecastService = weatherForecastService;
            _logger = logger;
        }

        // GET: api/WeatherForecast
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WeatherForecast>), 200)]
        [ProducesResponseType(typeof(api.DTOs.ErrorResponse), 401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<WeatherForecast>>> GetAll()
        {
            try
            {
                var forecasts = await _weatherForecastService.GetAllAsync();
                return Ok(forecasts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des prévisions météo");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/WeatherForecast/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(WeatherForecast), 200)]
        [ProducesResponseType(typeof(api.DTOs.ErrorResponse), 401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<WeatherForecast>> GetById(int id)
        {
            try
            {
                var forecast = await _weatherForecastService.GetByIdAsync(id);
                
                if (forecast == null)
                {
                    return NotFound($"Prévision avec l'ID {id} introuvable");
                }

                return Ok(forecast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la prévision {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // NOTE: Les routes POST, PUT et DELETE sont désactivées
        // L'API est en lecture seule pour les utilisateurs externes
        // La gestion des données se fait via l'application Web
        
        // POST: api/WeatherForecast
        /// <summary>
        /// Créer une nouvelle prévision météo
        /// </summary>
        /// <param name="request">Données de la prévision (sans ID, auto-généré)</param>
        /// <returns>La prévision créée avec son ID</returns>
        [HttpPost]
        [ProducesResponseType(typeof(WeatherForecast), 201)]
        [ProducesResponseType(typeof(api.DTOs.ErrorResponse), 400)]
        [ProducesResponseType(typeof(api.DTOs.ErrorResponse), 401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<WeatherForecast>> Create([FromBody] api.DTOs.CreateWeatherForecastRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Mapper le DTO vers l'entité avec le Value Object Temperature
                var temperature = new Temperature(request.TemperatureC);
                var forecast = new WeatherForecast(request.Date, temperature, request.Summary);

                var createdForecast = await _weatherForecastService.CreateAsync(forecast);

                _logger.LogInformation("Prévision météo créée via API : {Id}", createdForecast.Id);

                return CreatedAtAction(nameof(GetById), new { id = createdForecast.Id }, createdForecast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la prévision météo");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // PUT: api/WeatherForecast/5
        /// <summary>
        /// Mettre à jour une prévision météo existante
        /// </summary>
        /// <param name="id">ID de la prévision à mettre à jour</param>
        /// <param name="request">Nouvelles données (sans ID, passé dans l'URL)</param>
        /// <returns>204 No Content si succès</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(api.DTOs.ErrorResponse), 400)]
        [ProducesResponseType(typeof(api.DTOs.ErrorResponse), 401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Update(int id, [FromBody] api.DTOs.UpdateWeatherForecastRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Mapper le DTO vers l'entité avec le Value Object Temperature
                var temperature = new Temperature(request.TemperatureC);
                var forecast = new WeatherForecast(request.Date, temperature, request.Summary)
                {
                    Id = id // ID vient de l'URL
                };

                var success = await _weatherForecastService.UpdateAsync(id, forecast);
                
                if (!success)
                {
                    return NotFound($"Prévision avec l'ID {id} introuvable");
                }

                _logger.LogInformation("Prévision météo mise à jour via API : {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la prévision {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // DELETE: api/WeatherForecast/5 (DÉSACTIVÉ)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _weatherForecastService.DeleteAsync(id);
                
                if (!success)
                {
                    return NotFound($"Prévision avec l'ID {id} introuvable");
                }

                _logger.LogInformation("Prévision météo supprimée via API : {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la prévision {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }
    }
}
