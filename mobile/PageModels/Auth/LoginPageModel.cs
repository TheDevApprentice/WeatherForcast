using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mobile.Models.DTOs;
using mobile.Services;

namespace mobile.PageModels.Auth
{
    public partial class LoginPageModel : ObservableObject
    {
        private readonly IApiService _apiService;
        private readonly ISecureStorageService _secureStorage;
        private readonly IAuthenticationStateService _authState;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasError;

        public bool IsNotLoading => !IsLoading;

        public LoginPageModel(
            IApiService apiService, 
            ISecureStorageService secureStorage,
            IAuthenticationStateService authState)
        {
            _apiService = apiService;
            _secureStorage = secureStorage;
            _authState = authState;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            // Réinitialiser l'erreur
            HasError = false;
            ErrorMessage = string.Empty;

            // Validation
            if (string.IsNullOrWhiteSpace(Email))
            {
                ShowError("Veuillez saisir votre email");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Veuillez saisir votre mot de passe");
                return;
            }

            try
            {
                IsLoading = true;

                var request = new LoginRequest
                {
                    Email = Email.Trim(),
                    Password = Password
                };

                var response = await _apiService.LoginAsync(request);

                if (response != null)
                {
                    // Sauvegarder le token et les infos utilisateur
                    await _secureStorage.SaveTokenAsync(response.Token);
                    await _secureStorage.SaveUserInfoAsync(response.Email, response.FirstName, response.LastName);

                    // Récupérer l'ID utilisateur depuis l'API
                    var currentUser = await _apiService.GetCurrentUserAsync();
                    
                    if (currentUser != null)
                    {
                        // Sauvegarder l'état d'authentification centralisé
                        var authState = Models.AuthenticationState.Authenticated(
                            currentUser.Id,
                            currentUser.Email,
                            currentUser.FirstName,
                            currentUser.LastName
                        );
                        await _authState.SetStateAsync(authState);
                    }

                    // Mettre à jour l'UI du Shell
                    if (Shell.Current is AppShell appShell)
                    {
                        appShell.UpdateAuthenticationUI(true);
                    }

                    // Navigation vers l'application principale
                    await Shell.Current.GoToAsync("///main");
                }
                else
                {
                    ShowError("Email ou mot de passe incorrect");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Erreur de connexion : {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToRegisterAsync()
        {
            await Shell.Current.GoToAsync("register");
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
