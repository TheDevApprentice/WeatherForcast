using Microsoft.Extensions.Logging;
using mobile.Models;

namespace mobile.Pages
{
    public partial class SplashPage : ContentPage
    {
        private readonly IStartupService _startupService;
        private readonly ISecureStorageService _secureStorage;
        private readonly ILogger<SplashPage> _logger;

        public SplashPage(
            IStartupService startupService,
            ISecureStorageService secureStorage,
            ILogger<SplashPage> logger)
        {
            InitializeComponent();
            _startupService = startupService;
            _secureStorage = secureStorage;
            _logger = logger;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Lancer les procédures de démarrage
            await ExecuteStartupAsync();
        }

        private async Task ExecuteStartupAsync()
        {
            try
            {
                var progress = new Progress<StartupProcedure>(UpdateStepView);
                var success = await _startupService.ExecuteStartupProceduresAsync(progress);

                if (success)
                {
                    // Toutes les procédures réussies
                    await Task.Delay(500); // Petit délai pour voir le succès
                    await NavigateToAppropriatePageAsync();
                }
                else
                {
                    // Une procédure a échoué
                    ShowError("Une erreur est survenue lors du démarrage de l'application.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution des procédures de démarrage");
                ShowError($"Erreur inattendue: {ex.Message}");
            }
        }

        private void UpdateStepView(StartupProcedure procedure)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Mettre à jour l'icône avec des glyphes Segoe Fluent Icons
                StatusIcon.Text = procedure.Status switch
                {
                    StartupProcedureStatus.Running => "\uE895", // Sync icon
                    StartupProcedureStatus.Success => "\uE73E", // CheckMark icon
                    StartupProcedureStatus.Failed => "\uE711",  // Error icon
                    _ => ""
                };

                // Mettre à jour le texte de description
                CurrentStepLabel.Text = procedure.Description;

                // Mettre à jour les points de progression
                UpdateProgressDots();
            });
        }

        private void UpdateProgressDots()
        {
            var procedures = _startupService.Procedures;
            
            // Point 1 - Vérification réseau
            if (procedures.Count > 0)
            {
                Dot1.Fill = procedures[0].Status switch
                {
                    StartupProcedureStatus.Success => Colors.Green,
                    StartupProcedureStatus.Failed => Colors.Red,
                    StartupProcedureStatus.Running => Application.Current?.Resources["Primary"] as Color ?? Colors.Blue,
                    _ => (Application.Current?.Resources["Primary"] as Color ?? Colors.Blue).WithAlpha(0.3f)
                };
                Dot1.Opacity = procedures[0].Status == StartupProcedureStatus.Pending ? 0.3 : 1.0;
            }

            // Point 2 - Connexion API
            if (procedures.Count > 1)
            {
                Dot2.Fill = procedures[1].Status switch
                {
                    StartupProcedureStatus.Success => Colors.Green,
                    StartupProcedureStatus.Failed => Colors.Red,
                    StartupProcedureStatus.Running => Application.Current?.Resources["Primary"] as Color ?? Colors.Blue,
                    _ => (Application.Current?.Resources["Primary"] as Color ?? Colors.Blue).WithAlpha(0.3f)
                };
                Dot2.Opacity = procedures[1].Status == StartupProcedureStatus.Pending ? 0.3 : 1.0;
            }

            // Point 3 - Validation session
            if (procedures.Count > 2)
            {
                Dot3.Fill = procedures[2].Status switch
                {
                    StartupProcedureStatus.Success => Colors.Green,
                    StartupProcedureStatus.Failed => Colors.Red,
                    StartupProcedureStatus.Running => Application.Current?.Resources["Primary"] as Color ?? Colors.Blue,
                    _ => (Application.Current?.Resources["Primary"] as Color ?? Colors.Blue).WithAlpha(0.3f)
                };
                Dot3.Opacity = procedures[2].Status == StartupProcedureStatus.Pending ? 0.3 : 1.0;
            }
        }

        private void ShowError(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingIndicator.IsRunning = false;
                ErrorContainer.IsVisible = true;
                ErrorMessage.Text = message;
            });
        }

        private async void OnRetryClicked(object sender, EventArgs e)
        {
            ErrorContainer.IsVisible = false;
            LoadingIndicator.IsRunning = true;
            CurrentStepLabel.Text = "Démarrage...";
            await ExecuteStartupAsync();
        }

        private async Task NavigateToAppropriatePageAsync()
        {
            // Vérifier si l'utilisateur est authentifié
            var isAuthenticated = await _secureStorage.IsAuthenticatedAsync();

            _logger.LogInformation("Navigation vers {Page}", isAuthenticated ? "MainPage" : "LoginPage");

            // Naviguer vers la page appropriée
            if (isAuthenticated)
            {
                await Shell.Current.GoToAsync("///main");
            }
            else
            {
                await Shell.Current.GoToAsync("///login");
            }
        }
    }
}
