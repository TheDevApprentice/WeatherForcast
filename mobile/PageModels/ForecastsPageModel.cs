using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Models;
using mobile.Services;

namespace mobile.PageModels
{
    public partial class ForecastsPageModel : ObservableObject
    {
        private readonly IApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<WeatherForecast> forecasts = new();

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private int forecastsCount;

        public ForecastsPageModel(IApiService apiService)
        {
            _apiService = apiService;
            LoadForecasts();
        }

        private async void LoadForecasts()
        {
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
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les prévisions: {ex.Message}", "OK");
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
    }
}
