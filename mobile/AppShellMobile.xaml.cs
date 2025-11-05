namespace mobile
{
    public partial class AppShellMobile : Shell
    {
        private View? _offlineBannerView;
        private INetworkMonitorService? _networkMonitor;

        public AppShellMobile ()
        {
            InitializeComponent();

            // Les routes sont gérées par navigation directe avec Shell.Current.Navigation.PushAsync
            // au lieu de Routing.RegisterRoute car les pages utilisent l'injection de dépendances

            // Préparer la vue du bandeau hors ligne
            CreateOfflineBannerView();

            // Ré-appliquer le bandeau à chaque navigation
            this.Navigated += (_, __) => ApplyOfflineBannerForCurrentPage();
        }

        /// <summary>
        /// Initialise le NetworkMonitor (appelé depuis App.xaml.cs après que le Shell soit prêt)
        /// </summary>
        public void InitializeNetworkMonitor (INetworkMonitorService networkMonitor)
        {
            if (_networkMonitor != null)
                return; // Déjà initialisé

            _networkMonitor = networkMonitor;
            _networkMonitor.ConnectivityChanged += OnConnectivityChanged;

            // Vérifier l'état initial
            ApplyOfflineBannerForCurrentPage();
        }

        /// <summary>
        /// Crée le bandeau hors ligne qui sera affiché au-dessus de toutes les pages
        /// </summary>
        private void CreateOfflineBannerView ()
        {
            _offlineBannerView = new Border
            {
                BackgroundColor = Color.FromArgb("#FF9800"), // Orange
                HeightRequest = 44,
                Padding = new Thickness(12, 0),
                HorizontalOptions = LayoutOptions.Fill,
                Content = new HorizontalStackLayout
                {
                    Spacing = 10,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "⚠️",
                            FontSize = 20,
                            VerticalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "Vous êtes hors ligne",
                            FontSize = 14,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Colors.White,
                            VerticalOptions = LayoutOptions.Center
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Gestionnaire d'événement pour les changements de connectivité
        /// </summary>
        private void OnConnectivityChanged (object? sender, NetworkAccess access)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ApplyOfflineBannerForCurrentPage();
            });
        }

        /// <summary>
        /// Applique ou retire le bandeau hors ligne sur la page courante
        /// </summary>
        private void ApplyOfflineBannerForCurrentPage ()
        {
            if (_networkMonitor == null || _offlineBannerView == null)
                return;

            // Obtenir la page courante
            var currentPage = Shell.Current?.CurrentPage as ContentPage;
            if (currentPage == null)
                return;

            bool shouldShowBanner = !_networkMonitor.IsNetworkAvailable;

            // Vérifier si la page a déjà un Grid comme contenu
            if (currentPage.Content is Grid grid)
            {
                // Chercher si le bandeau existe déjà
                var existingBanner = grid.Children.FirstOrDefault(c => c == _offlineBannerView);

                if (shouldShowBanner && existingBanner == null)
                {
                    // Ajouter le bandeau en haut
                    grid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });

                    // Décaler tous les enfants existants d'une ligne
                    foreach (var child in grid.Children.Where(c => c != _offlineBannerView))
                    {
                        var currentRow = grid.GetRow(child);
                        grid.SetRow(child, currentRow + 1);
                    }

                    // Ajouter le bandeau à la ligne 0
                    Grid.SetRow(_offlineBannerView, 0);
                    grid.Children.Insert(0, _offlineBannerView);
                }
                else if (!shouldShowBanner && existingBanner != null)
                {
                    // Retirer le bandeau
                    grid.Children.Remove(_offlineBannerView);
                    grid.RowDefinitions.RemoveAt(0);

                    // Redécaler tous les enfants
                    foreach (var child in grid.Children)
                    {
                        var currentRow = grid.GetRow(child);
                        if (currentRow > 0)
                            grid.SetRow(child, currentRow - 1);
                    }
                }
            }
            else if (shouldShowBanner)
            {
                // La page n'a pas de Grid, créer un wrapper
                var originalContent = currentPage.Content;
                var wrapperGrid = new Grid
                {
                    RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Auto }, // Bandeau
                        new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } // Contenu original
                    }
                };

                Microsoft.Maui.Controls.Grid.SetRow(_offlineBannerView, 0);
                wrapperGrid.Children.Add(_offlineBannerView);

                if (originalContent != null)
                {
                    Microsoft.Maui.Controls.Grid.SetRow(originalContent, 1);
                    wrapperGrid.Children.Add(originalContent);
                }

                currentPage.Content = wrapperGrid;
            }
        }
    }
}
