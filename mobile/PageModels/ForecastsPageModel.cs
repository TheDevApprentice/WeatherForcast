using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Models;
using mobile.Services;

namespace mobile.PageModels
{
    public partial class ForecastsPageModel : ObservableObject, IDisposable
    {
        private readonly IApiService _apiService;
        private readonly ISignalRService _signalRService;
        private readonly INotificationService _notificationService;
        private readonly IErrorHandler _errorHandler;

        // Déduplication des notifications (éviter les doublons)
        private readonly HashSet<string> _processedNotifications = new();
        private readonly TimeSpan _deduplicationWindow = TimeSpan.FromSeconds(2);
        private bool _disposed = false;

        [ObservableProperty]
        private ObservableCollection<WeatherForecast> forecasts = new();

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private int forecastsCount;

        public ForecastsPageModel(
            IApiService apiService, 
            ISignalRService signalRService,
            INotificationService notificationService,
            IErrorHandler errorHandler)
        {
            _apiService = apiService;
            _signalRService = signalRService;
            _notificationService = notificationService;
            _errorHandler = errorHandler;

            // Ne pas s'abonner ici, le faire dans OnAppearing
            _ = InitializeAsync(); // Fire and forget avec intention claire
        }

        /// <summary>
        /// Appelé quand la page apparaît
        /// </summary>
        public void OnAppearing()
        {
            if (_disposed)
            {
                _disposed = false;
            }

            // S'abonner aux événements SignalR
            _signalRService.ForecastCreated -= OnForecastCreated; // Désabonner d'abord (au cas où)
            _signalRService.ForecastUpdated -= OnForecastUpdated;
            _signalRService.ForecastDeleted -= OnForecastDeleted;

            _signalRService.ForecastCreated += OnForecastCreated; // Puis réabonner
            _signalRService.ForecastUpdated += OnForecastUpdated;
            _signalRService.ForecastDeleted += OnForecastDeleted;
        }

        /// <summary>
        /// Appelé quand la page disparaît
        /// </summary>
        public void OnDisappearing()
        {
            // Désabonner les événements SignalR
            _signalRService.ForecastCreated -= OnForecastCreated;
            _signalRService.ForecastUpdated -= OnForecastUpdated;
            _signalRService.ForecastDeleted -= OnForecastDeleted;
        }

        private async Task InitializeAsync()
        {
            // Démarrer la connexion SignalR au hub des forecasts
            await _signalRService.StartForecastHubAsync();
            
            await LoadForecastsAsync();
        }

        private async Task LoadForecastsAsync()
        {
            try
            {
                IsRefreshing = true;

                // Récupérer les prévisions depuis l'API
                var forecastsList = await _apiService.GetForecastsAsync();

                Forecasts.Clear();
                foreach (var forecast in forecastsList)
                {
                    Forecasts.Add(forecast);
                }

                ForecastsCount = Forecasts.Count;
            }
            catch (Exception ex)
            {
                // Utiliser le gestionnaire d'erreurs au lieu de DisplayAlert
                await _errorHandler.HandleErrorWithMessageAsync(
                    ex,
                    "Impossible de charger les prévisions. Vérifiez votre connexion Internet.");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadForecastsAsync();
        }

        private async void OnForecastCreated(object? sender, Models.WeatherForecast forecast)
        {
            // Déduplication: vérifier si on a déjà traité cette notification récemment
            var notificationKey = $"created_{forecast.Id}_{DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond}";
            if (!_processedNotifications.Add(notificationKey))
            {
                return; // Notification déjà traitée
            }

            // Nettoyer les anciennes notifications (garder seulement les 2 dernières secondes)
            CleanupOldNotifications();

            // Ajouter le nouveau forecast à la liste
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Vérifier si le forecast n'existe pas déjà
                if (!Forecasts.Any(f => f.Id == forecast.Id))
                {
                    Forecasts.Add(forecast);
                    ForecastsCount = Forecasts.Count;
                }
            });

            // Afficher une notification toast
            await _notificationService.ShowForecastCreatedAsync(forecast);
        }

        private async void OnForecastUpdated(object? sender, Models.WeatherForecast forecast)
        {
            // Déduplication: vérifier si on a déjà traité cette notification récemment
            var notificationKey = $"updated_{forecast.Id}_{DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond}";
            if (!_processedNotifications.Add(notificationKey))
            {
                return; // Notification déjà traitée
            }

            // Nettoyer les anciennes notifications
            CleanupOldNotifications();

            // Mettre à jour le forecast dans la liste
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var existingForecast = Forecasts.FirstOrDefault(f => f.Id == forecast.Id);
                if (existingForecast != null)
                {
                    var index = Forecasts.IndexOf(existingForecast);
                    Forecasts[index] = forecast;
                }
            });

            // Afficher une notification toast
            await _notificationService.ShowForecastUpdatedAsync(forecast);
        }

        private async void OnForecastDeleted(object? sender, int id)
        {
            // Déduplication: vérifier si on a déjà traité cette notification récemment
            var notificationKey = $"deleted_{id}_{DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond}";
            if (!_processedNotifications.Add(notificationKey))
            {
                return; // Notification déjà traitée
            }

            // Nettoyer les anciennes notifications
            CleanupOldNotifications();

            // Supprimer le forecast de la liste
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var forecast = Forecasts.FirstOrDefault(f => f.Id == id);
                if (forecast != null)
                {
                    Forecasts.Remove(forecast);
                    ForecastsCount = Forecasts.Count;
                }
            });

            // Afficher une notification toast
            await _notificationService.ShowForecastDeletedAsync(id);
        }

        /// <summary>
        /// Nettoie les anciennes notifications pour éviter une fuite mémoire
        /// </summary>
        private void CleanupOldNotifications()
        {
            // Garder seulement les notifications des 10 dernières secondes
            if (_processedNotifications.Count > 100)
            {
                _processedNotifications.Clear();
            }
        }

        /// <summary>
        /// Dispose des ressources et désabonne les événements SignalR
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            // Désabonner les événements SignalR pour éviter les fuites mémoire
            _signalRService.ForecastCreated -= OnForecastCreated;
            _signalRService.ForecastUpdated -= OnForecastUpdated;
            _signalRService.ForecastDeleted -= OnForecastDeleted;

            _disposed = true;
        }
    }
}
