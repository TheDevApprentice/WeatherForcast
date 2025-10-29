using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Models.DTOs;
using mobile.Services;
using System.Text.RegularExpressions;

namespace mobile.PageModels.Auth
{
    public partial class RegisterPageModel : ObservableObject
    {
        private readonly IApiService _apiService;
        private readonly ISecureStorageService _secureStorage;

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

        public RegisterPageModel(IApiService apiService, ISecureStorageService secureStorage)
        {
            _apiService = apiService;
            _secureStorage = secureStorage;
        }

        [RelayCommand]
        private async Task RegisterAsync()
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

                var request = new RegisterRequest
                {
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Email = Email.Trim(),
                    Password = Password
                };

                var response = await _apiService.RegisterAsync(request);

                if (response != null)
                {
                    // Sauvegarder le token et les infos utilisateur
                    await _secureStorage.SaveTokenAsync(response.Token);
                    await _secureStorage.SaveUserInfoAsync(response.Email, response.FirstName, response.LastName);

                    // Navigation vers l'application principale
                    await Shell.Current.GoToAsync("///main");
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
        private async Task NavigateToLoginAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private bool ValidateInputs()
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

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        partial void OnIsLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotLoading));
        }
    }
}
