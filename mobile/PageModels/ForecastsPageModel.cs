using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Services.Api.Interfaces;
using mobile.Services.Notifications.Interfaces;
using System.Collections.ObjectModel;

namespace mobile.PageModels
{
    public partial class ForecastsPageModel : ObservableObject, IDisposable
    {
        private readonly IApiWeatherForecastService _apiWeatherForecastService;
        private readonly ISignalRService _signalRService;
        private readonly INotificationService _notificationService;

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

        public ForecastsPageModel (
            IApiWeatherForecastService apiWeatherForecastService,
            ISignalRService signalRService,
            INotificationService notificationService)
        {
            _apiWeatherForecastService = apiWeatherForecastService;
            _signalRService = signalRService;
            _notificationService = notificationService;

            // Ne pas initialiser ici, le faire dans OnAppearing
        }

        /// <summary>
        /// Appelé quand la page apparaît
        /// </summary>
        public async void OnAppearing ()
        {
            // ForecastsPageModel.OnAppearing() appelé

            if (_disposed)
            {
                _disposed = false;
            }

            // ✅ Réinitialiser le gestionnaire de notifications pour cette page
            _notificationService.Reset();

            // S'abonner aux événements SignalR
            _signalRService.ForecastCreated -= OnForecastCreated; // Désabonner d'abord (au cas où)
            _signalRService.ForecastUpdated -= OnForecastUpdated;
            _signalRService.ForecastDeleted -= OnForecastDeleted;

            _signalRService.ForecastCreated += OnForecastCreated; // Puis réabonner
            _signalRService.ForecastUpdated += OnForecastUpdated;
            _signalRService.ForecastDeleted += OnForecastDeleted;

            // Événements SignalR abonnés

            // Initialiser la connexion SignalR et charger les données
            await InitializeAsync();
        }

        /// <summary>
        /// Appelé quand la page disparaît
        /// </summary>
        public void OnDisappearing ()
        {
            // Désabonner les événements SignalR
            _signalRService.ForecastCreated -= OnForecastCreated;
            _signalRService.ForecastUpdated -= OnForecastUpdated;
            _signalRService.ForecastDeleted -= OnForecastDeleted;
        }

        private async Task InitializeAsync ()
        {
            try
            {
                // Démarrer la connexion SignalR au hub des forecasts
                await _signalRService.StartForecastHubAsync();
            }
            catch (Exception ex)
            {
                // Si SignalR échoue, continuer quand même (on aura juste pas le temps réel)
#if DEBUG
                await Shell.Current.DisplayAlert("Debug ForecastsPageModel", $"❌ SignalR connection failed: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                await _notificationService.ShowErrorAsync("SignalR", $"⚠️ SignalR connection failed: {ex.Message}");
            }

            // Charger les prévisions depuis l'API (même si SignalR a échoué)
            await LoadForecastsAsync();
        }

        private async Task LoadForecastsAsync ()
        {
            try
            {
                IsRefreshing = true;

                // Récupérer les prévisions depuis l'API
                var forecastsList = await _apiWeatherForecastService.GetForecastsAsync();

                // Mettre à jour la collection sur le thread UI
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Forecasts.Clear();
                    foreach (var forecast in forecastsList)
                    {
                        Forecasts.Add(forecast);
                    }

                    ForecastsCount = Forecasts.Count;
                });
            }
            catch (Exception ex)
            {
                // await _notificationService.ShowErrorAsync(ex.Message, "Impossible de charger les prévisions.");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync ()
        {
            await LoadForecastsAsync();
        }

        private async void OnForecastCreated (object? sender, Models.WeatherForecast forecast)
        {
            // SignalR: OnForecastCreated appelé

            // Déduplication: vérifier si on a déjà traité cette notification récemment
            var notificationKey = $"created_{forecast.Id}_{DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond}";
            if (!_processedNotifications.Add(notificationKey))
            {
                // Notification dupliquée ignorée
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
                    // Forecast ajouté à la liste
                }
            });

            // Afficher une notification toast
            await _notificationService.ShowForecastCreatedAsync(forecast);
        }

        private async void OnForecastUpdated (object? sender, Models.WeatherForecast forecast)
        {
            // SignalR: OnForecastUpdated appelé

            // Déduplication: vérifier si on a déjà traité cette notification récemment
            var notificationKey = $"updated_{forecast.Id}_{DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond}";
            if (!_processedNotifications.Add(notificationKey))
            {
                // Notification dupliquée ignorée
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

        private async void OnForecastDeleted (object? sender, int id)
        {
            // SignalR: OnForecastDeleted appelé

            // Déduplication: vérifier si on a déjà traité cette notification récemment
            var notificationKey = $"deleted_{id}_{DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond}";
            if (!_processedNotifications.Add(notificationKey))
            {
                // Notification dupliquée ignorée
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
                    // Suppression du forecast de la liste
                    Forecasts.Remove(forecast);
                    ForecastsCount = Forecasts.Count;
                }
                else
                {
                    // Forecast introuvable dans la liste
                }
            });

            // Afficher une notification toast
            await _notificationService.ShowForecastDeletedAsync(id);
        }

        /// <summary>
        /// Nettoie les anciennes notifications pour éviter une fuite mémoire
        /// </summary>
        private void CleanupOldNotifications ()
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
        public void Dispose ()
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
