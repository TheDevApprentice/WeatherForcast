using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Services.Internal.Interfaces;
using System.Collections.ObjectModel;

namespace mobile.PageModels
{
    /// <summary>
    /// PageModel pour la page Splash
    /// Gère les procédures de démarrage de l'application
    /// </summary>
    public partial class SplashPageModel : ObservableObject
    {
        private readonly IStartupService _startupService;
        private readonly ISecureStorageService _secureStorage;
        private readonly IAuthenticationStateService _authState;

        [ObservableProperty]
        private string _currentStepDescription = "Démarrage...";

        [ObservableProperty]
        private string _statusIcon = "";

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private bool _hasError = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<StartupProcedure> _procedures = new();

        [ObservableProperty]
        private int _currentStepIndex = 0;

        /// <summary>
        /// Événement déclenché lorsque le démarrage est terminé avec succès
        /// </summary>
        public event EventHandler<bool>? StartupCompleted;

        public SplashPageModel(
            IStartupService startupService,
            ISecureStorageService secureStorage,
            IAuthenticationStateService authState)
        {
            _startupService = startupService;
            _secureStorage = secureStorage;
            _authState = authState;
        }

        /// <summary>
        /// Exécute les procédures de démarrage
        /// </summary>
        [RelayCommand]
        public async Task ExecuteStartupAsync()
        {
            try
            {
                HasError = false;
                IsLoading = true;

                var progress = new Progress<StartupProcedure>(UpdateStepView);
                var success = await _startupService.ExecuteStartupProceduresAsync(progress);

                if (success)
                {
                    await Task.Delay(500); // Petit délai pour voir le succès
                    
                    // Notifier que le démarrage est terminé avec succès
                    StartupCompleted?.Invoke(this, true);
                }
                else
                {
                    ShowError("Une erreur est survenue lors du démarrage de l'application.");
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SplashPageModel", $"❌ Erreur lors de l'exécution des procédures de démarrage: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
                ShowError($"Erreur inattendue: {ex.Message}");
            }
        }

        /// <summary>
        /// Met à jour l'affichage de l'étape en cours
        /// </summary>
        private void UpdateStepView(StartupProcedure procedure)
        {
            StatusIcon = procedure.Status switch
            {
                StartupProcedureStatus.Running => "\uF16A", // Sync icon
                StartupProcedureStatus.Success => "\uE930", // CheckMark icon
                StartupProcedureStatus.Failed => "\uEB90",  // Error icon
                _ => ""
            };

            CurrentStepDescription = procedure.Description;

            // Mettre à jour la collection des procédures
            Procedures = new ObservableCollection<StartupProcedure>(_startupService.Procedures);

            // Mettre à jour l'index de l'étape actuelle
            var completedCount = _startupService.Procedures.Count(p => p.Status == StartupProcedureStatus.Success);
            CurrentStepIndex = completedCount;
        }

        /// <summary>
        /// Affiche une erreur
        /// </summary>
        private void ShowError(string message)
        {
            IsLoading = false;
            HasError = true;
            ErrorMessage = message;
        }

        /// <summary>
        /// Réessaye le démarrage après une erreur
        /// </summary>
        [RelayCommand]
        public async Task RetryAsync()
        {
            HasError = false;
            IsLoading = true;
            CurrentStepDescription = "Démarrage...";
            await ExecuteStartupAsync();
        }

        /// <summary>
        /// Détermine vers quelle page naviguer après le démarrage
        /// </summary>
        public async Task<(bool isAuthenticated, bool shouldShowTitleBar)> GetNavigationInfoAsync()
        {
            var authState = await _authState.GetStateAsync();
            // Navigation vers MainPage ou LoginPage selon l'état d'authentification
            return (authState.IsAuthenticated, authState.IsAuthenticated);
        }
    }
}
