using Microsoft.Maui.Controls.Shapes;
using mobile.PageModels;

namespace mobile.Pages
{
    public partial class SplashPage : ContentPage
    {
        private readonly SplashPageModel _viewModel;
        private readonly List<Ellipse> _dots = new();

        public SplashPage(SplashPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
            
            // S'abonner aux changements de l'index de l'étape pour mettre à jour les dots
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            
            // S'abonner à l'événement de fin de démarrage pour la navigation
            _viewModel.StartupCompleted += OnStartupCompleted;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SplashPageModel.Procedures))
            {
                // Recréer les dots si le nombre de procédures change
                InitializeProgressDots();
            }
            else if (e.PropertyName == nameof(SplashPageModel.CurrentStepIndex))
            {
                // Mettre à jour les dots existants
                UpdateProgressDots();
            }
        }

        /// <summary>
        /// Initialise les dots de progression en fonction du nombre de procédures
        /// </summary>
        private void InitializeProgressDots()
        {
            var procedures = _viewModel.Procedures.ToList();
            
            // Si le nombre de dots correspond déjà, ne rien faire
            if (_dots.Count == procedures.Count)
            {
                UpdateProgressDots();
                return;
            }

            // Vider le conteneur et la liste
            DotsContainer.Children.Clear();
            _dots.Clear();

            // Créer un dot pour chaque procédure
            for (int i = 0; i < procedures.Count; i++)
            {
                var dot = new Ellipse
                {
                    Fill = Application.Current?.Resources["Primary"] as Brush ?? new SolidColorBrush(Colors.Blue),
                    WidthRequest = 8,
                    HeightRequest = 8,
                    Opacity = 0.3
                };

                _dots.Add(dot);
                DotsContainer.Children.Add(dot);
            }

            // Mettre à jour l'état initial
            UpdateProgressDots();
        }

        /// <summary>
        /// Met à jour l'état visuel des dots selon le statut des procédures
        /// </summary>
        private void UpdateProgressDots()
        {
            var procedures = _viewModel.Procedures.ToList();

            // Mettre à jour chaque dot selon le statut de sa procédure
            for (int i = 0; i < _dots.Count && i < procedures.Count; i++)
            {
                var dot = _dots[i];
                var procedure = procedures[i];

                // Définir la couleur selon le statut
                dot.Fill = procedure.Status switch
                {
                    StartupProcedureStatus.Success => new SolidColorBrush(Colors.Green),
                    StartupProcedureStatus.Failed => new SolidColorBrush(Colors.Red),
                    StartupProcedureStatus.Running => Application.Current?.Resources["Primary"] as Brush ?? new SolidColorBrush(Colors.Blue),
                    _ => Application.Current?.Resources["Primary"] as Brush ?? new SolidColorBrush(Colors.Blue)
                };

                // Définir l'opacité (transparent si pending, opaque sinon)
                dot.Opacity = procedure.Status == StartupProcedureStatus.Pending ? 0.3 : 1.0;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Les éléments de la title bar sont déjà masqués dans App.xaml.cs
            // Lancer les procédures de démarrage via la commande du ViewModel
            await _viewModel.ExecuteStartupCommand.ExecuteAsync(null);
        }

        /// <summary>
        /// Gestionnaire de l'événement de fin de démarrage
        /// </summary>
        private async void OnStartupCompleted(object? sender, bool success)
        {
            if (success)
            {
                await NavigateToAppropriatePageAsync();
            }
        }

        private async Task NavigateToAppropriatePageAsync()
        {
            // Récupérer les informations de navigation depuis le ViewModel
            var (isAuthenticated, shouldShowTitleBar) = await _viewModel.GetNavigationInfoAsync();

            // Mettre à jour l'UI du Shell (sans toucher à la title bar)
            if (Shell.Current is AppShell shell)
            {
                shell.UpdateAuthenticationUI(isAuthenticated, updateTitleBar: false);
            }

            // Afficher les éléments de la title bar après le splash
#if WINDOWS || MACCATALYST
            if (Application.Current?.Windows?.Count > 0 && Application.Current.Windows[0] is MainWindow mw)
            {
                mw.ShowTitleBarElements(shouldShowTitleBar);
            }
#endif

            // Naviguer vers la page appropriée
            if (isAuthenticated)
            {
#if ANDROID || IOS
                // Sur mobile : fermer le splash modal, réafficher le TabBar
                Shell.SetTabBarIsVisible(Shell.Current, true);
                await Navigation.PopModalAsync(false);
                // Le TabBar affichera automatiquement le premier onglet (Dashboard)
#else
                // Sur desktop : navigation globale
                await Shell.Current.GoToAsync("///main");
#endif
            }
            else
            {
#if ANDROID || IOS
                // Sur mobile : remplacer splash par login (modal)
                var loginPage = Handler?.MauiContext?.Services.GetService<Auth.LoginPage>();
                if (loginPage != null)
                {
                    await Navigation.PopModalAsync(false); // Fermer splash
                    await Shell.Current.Navigation.PushModalAsync(loginPage, false); // Ouvrir login
                }
#else
                // Sur desktop : navigation globale
                await Shell.Current.GoToAsync("///login");
#endif
            }
        }
    }
}
