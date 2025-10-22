using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using domain.Entities;
using domain.Interfaces.Services;

namespace application.Controllers
{
    /// <summary>
    /// Controller WeatherForecast avec Clean Architecture
    /// Utilise IWeatherForecastService (pas de dépendance directe à Infrastructure)
    /// Nécessite une authentification
    /// Les notifications temps réel sont gérées automatiquement par les Domain Events (MediatR)
    /// </summary>
    [Authorize]
    public class WeatherForecastController : Controller
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

        // GET: /WeatherForecast
        public async Task<IActionResult> Index()
        {
            try
            {
                var forecasts = await _weatherForecastService.GetAllAsync();
                return View(forecasts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des prévisions météo");
                return View("Error");
            }
        }

        // GET: /WeatherForecast/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var forecast = await _weatherForecastService.GetByIdAsync(id);
            
            if (forecast == null)
            {
                return NotFound();
            }

            return View(forecast);
        }

        // GET: /WeatherForecast/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /WeatherForecast/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Date,TemperatureC,Summary")] WeatherForecast forecast)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _weatherForecastService.CreateAsync(forecast);
                    
                    _logger.LogInformation("Prévision météo créée avec succès : {Id}", forecast.Id);
                    
                    // La notification SignalR est automatiquement gérée par le SignalRForecastNotificationHandler
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la création de la prévision météo");
                    ModelState.AddModelError("", "Une erreur est survenue lors de la création.");
                }
            }

            return View(forecast);
        }

        // GET: /WeatherForecast/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var forecast = await _weatherForecastService.GetByIdAsync(id);
            
            if (forecast == null)
            {
                return NotFound();
            }

            return View(forecast);
        }

        // POST: /WeatherForecast/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,TemperatureC,Summary")] WeatherForecast forecast)
        {
            if (id != forecast.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _weatherForecastService.UpdateAsync(id, forecast);
                    
                    _logger.LogInformation("Prévision météo mise à jour : {Id}", forecast.Id);
                    
                    // La notification SignalR est automatiquement gérée par le SignalRForecastNotificationHandler
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la mise à jour de la prévision météo");
                    ModelState.AddModelError("", "Une erreur est survenue lors de la mise à jour.");
                }
            }

            return View(forecast);
        }

        // GET: /WeatherForecast/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var forecast = await _weatherForecastService.GetByIdAsync(id);
            
            if (forecast == null)
            {
                return NotFound();
            }

            return View(forecast);
        }

        // POST: /WeatherForecast/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await _weatherForecastService.DeleteAsync(id);
                
                if (!success)
                {
                    return NotFound();
                }
                
                _logger.LogInformation("Prévision météo supprimée : {Id}", id);
                
                // La notification SignalR est automatiquement gérée par le SignalRForecastNotificationHandler
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la prévision météo");
                TempData["ErrorMessage"] = "Erreur lors de la suppression";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
