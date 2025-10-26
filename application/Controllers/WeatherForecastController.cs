using application.Authorization;
using application.Helpers;
using application.ViewModels;
using domain.Constants;
using domain.Entities;
using domain.Events;
using domain.Exceptions;
using domain.Interfaces.Services;
using domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IPublisher _publisher;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(
            IWeatherForecastService weatherForecastService,
            IPublisher publisher,
            ILogger<WeatherForecastController> logger)
        {
            _weatherForecastService = weatherForecastService;
            _publisher = publisher;
            _logger = logger;
        }

        // GET: /WeatherForecast
        [HasPermission(AppClaims.ForecastRead)]
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
        [HasPermission(AppClaims.ForecastRead)]
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
        [HasPermission(AppClaims.ForecastWrite)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /WeatherForecast/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(AppClaims.ForecastWrite)]
        public async Task<IActionResult> Create(WeatherForecastViewModel viewModel)
        {
            // ✅ Validation FluentValidation via ModelState
            if (!ModelState.IsValid)
            {
                // Publier l'erreur pour notification SignalR
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                await _publisher.PublishValidationErrorAsync(
                    User,
                    errors,
                    "Create",
                    "WeatherForecast",
                    null,
                    null);

                return View(viewModel);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Mapper le ViewModel vers l'entité avec le Value Object Temperature
                    var temperature = new Temperature(viewModel.TemperatureC);
                    var forecast = new WeatherForecast(viewModel.Date, temperature, viewModel.Summary);

                    await _weatherForecastService.CreateAsync(forecast);

                    _logger.LogInformation("Prévision météo créée avec succès : {Id}", forecast.Id);

                    // La notification SignalR est automatiquement gérée par le SignalRForecastNotificationHandler

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la création de la prévision météo");
                    var errorMessage = "Une erreur est survenue lors de la création de la prévision.";
                    ModelState.AddModelError("", errorMessage);

                    // Publier l'erreur pour notification temps réel
                    await _publisher.PublishGenericErrorAsync(
                        User,
                        errorMessage,
                        "Create",
                        "WeatherForecast",
                        null,
                        ex);

                    return View(viewModel);
                }
            }

            return View(viewModel);
        }

        // GET: /WeatherForecast/Edit/5
        [HasPermission(AppClaims.ForecastWrite)]
        public async Task<IActionResult> Edit(int id)
        {
            var forecast = await _weatherForecastService.GetByIdAsync(id);

            if (forecast == null)
            {
                return NotFound();
            }

            // Mapper l'entité vers le ViewModel
            var viewModel = new WeatherForecastViewModel
            {
                Id = forecast.Id,
                Date = forecast.Date,
                TemperatureC = forecast.TemperatureC,
                Summary = forecast.Summary
            };

            return View(viewModel);
        }

        // POST: /WeatherForecast/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(AppClaims.ForecastWrite)]
        public async Task<IActionResult> Edit(int id, WeatherForecastViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            // ✅ Validation FluentValidation via ModelState
            if (!ModelState.IsValid)
            {
                // Publier l'erreur pour notification SignalR
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                await _publisher.PublishValidationErrorAsync(
                    User,
                    errors,
                    "Update",
                    "WeatherForecast",
                    id.ToString(),
                    null);

                return View(viewModel);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Créer le Value Object Temperature
                    var temperature = new Temperature(viewModel.TemperatureC);

                    // Appeler le service avec les valeurs individuelles
                    await _weatherForecastService.UpdateAsync(id, viewModel.Date, temperature, viewModel.Summary);

                    _logger.LogInformation("Prévision météo mise à jour : {Id}", id);

                    // La notification SignalR est automatiquement gérée par le SignalRForecastNotificationHandler

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la mise à jour de la prévision météo");
                    var errorMessage = "Une erreur est survenue lors de la mise à jour de la prévision.";
                    ModelState.AddModelError("", errorMessage);

                    // Publier l'erreur pour notification temps réel
                    await _publisher.PublishGenericErrorAsync(
                        User,
                        errorMessage,
                        "Update",
                        "WeatherForecast",
                        id.ToString(),
                        ex);

                    return View(viewModel);
                }
            }

            return View(viewModel);
        }

        // GET: /WeatherForecast/Delete/5
        [HasPermission(AppClaims.ForecastDelete)]
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
        [HasPermission(AppClaims.ForecastDelete)]
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
            catch (DomainException ex)
            {
                // Exception typée du domain - publier directement
                _logger.LogError(ex, "Erreur domain lors de la suppression");
                TempData["ErrorMessage"] = ex.Message;

                await _publisher.PublishDomainExceptionAsync(User, ex);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Exception non gérée
                _logger.LogError(ex, "Erreur inattendue lors de la suppression");
                var errorMessage = "Une erreur inattendue est survenue.";
                TempData["ErrorMessage"] = errorMessage;

                await _publisher.PublishGenericErrorAsync(
                    User,
                    errorMessage,
                    "Delete",
                    "WeatherForecast",
                    id.ToString(),
                    ex);

                return RedirectToAction(nameof(Index));
            }
        }
    }
}
