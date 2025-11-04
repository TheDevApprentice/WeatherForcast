using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Models.DTOs;
using System.Text.RegularExpressions;

namespace mobile.PageModels.Auth
{
    public partial class RegisterPageModel : ObservableObject
    {
        private readonly IApiAuthService _apiAuthService;
        private readonly ISecureStorageService _secureStorage;
        private readonly ISignalRService _signalRService;
        private readonly INotificationService _notificationService;

        [ObservableProperty]
        private string firstName = string.Empty;

        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasError;

        public bool IsNotLoading => !IsLoading;

        public RegisterPageModel (
            IApiAuthService apiAuthService,
            ISecureStorageService secureStorage,
            ISignalRService signalRService,
            INotificationService notificationService)
        {
            _apiAuthService = apiAuthService;
            _secureStorage = secureStorage;
            _signalRService = signalRService;
            _notificationService = notificationService;

            // S'abonner aux événements SignalR
            _signalRService.VerificationEmailSent += OnVerificationEmailSent;
        }

        [RelayCommand]
        private async Task RegisterAsync ()
        {
            // Réinitialiser l'erreur
            HasError = false;
            ErrorMessage = string.Empty;

            // Validation
            if (!ValidateInputs())
            {
                return;
            }

            try
            {
                IsLoading = true;

                // Démarrer la connexion SignalR pour recevoir les notifications
                await _signalRService.StartUsersHubAsync();
                await _signalRService.JoinEmailChannelAsync(Email.Trim());

                var request = new RegisterRequest
                {
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Email = Email.Trim(),
                    Password = Password
                };

                var success = await _apiAuthService.RegisterAsync(request);

                if (success)
                {
                    // L'inscription a réussi, mais pas de token retourné
                    // L'utilisateur doit maintenant se connecter
                    ShowError("Compte créé avec succès ! Vérifiez votre email.");

                    // Attendre un peu pour recevoir la notification SignalR
                    await Task.Delay(3000);

                    // Quitter le canal email
                    await _signalRService.LeaveEmailChannelAsync(Email.Trim());

                    // Navigation vers la page de connexion
                    // Utiliser navigation relative (fonctionne sur toutes les plateformes)
                    await Shell.Current.GoToAsync("//login");
                }
                else
                {
                    ShowError("Erreur lors de la création du compte. Vérifiez vos informations.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Erreur : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToLoginAsync ()
        {
            // Navigation relative (fonctionne sur toutes les plateformes)
            await Shell.Current.GoToAsync("//login");
        }

        private bool ValidateInputs ()
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                ShowError("Veuillez saisir votre prénom");
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                ShowError("Veuillez saisir votre nom");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ShowError("Veuillez saisir votre email");
                return false;
            }

            // Validation email
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(Email))
            {
                ShowError("Format d'email invalide");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Veuillez saisir un mot de passe");
                return false;
            }

            if (Password.Length < 6)
            {
                ShowError("Le mot de passe doit contenir au moins 6 caractères");
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ShowError("Les mots de passe ne correspondent pas");
                return false;
            }

            return true;
        }

        private void ShowError (string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        partial void OnIsLoadingChanged (bool value)
        {
            OnPropertyChanged(nameof(IsNotLoading));
        }

        private async void OnVerificationEmailSent (object? sender, EmailNotification notification)
        {
            // Afficher une notification toast à l'utilisateur
            await _notificationService.ShowSuccessAsync(
                notification.Message ?? "Email de vérification envoyé avec succès",
                "Vérification Email");
        }
    }
}
