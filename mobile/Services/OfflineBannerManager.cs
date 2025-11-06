using mobile.Views;

namespace mobile
{
    /// <summary>
    /// Gestionnaire centralisé du bandeau hors ligne
    /// </summary>
    public class OfflineBannerManager : IOfflineBannerManager
    {
        private INetworkMonitorService? _networkMonitor;
        private readonly Dictionary<ContentPage, OfflineBanner> _pageBanners = new();

        /// <summary>
        /// Initialise le gestionnaire avec le service de monitoring réseau
        /// </summary>
        public void Initialize(INetworkMonitorService networkMonitor)
        {
            if (_networkMonitor != null)
                return; // Déjà initialisé

            _networkMonitor = networkMonitor;
            _networkMonitor.ConnectivityChanged += OnConnectivityChanged;
        }

        /// <summary>
        /// Applique le bandeau sur la page courante
        /// </summary>
        public void ApplyToCurrentPage()
        {
            if (_networkMonitor == null)
                return;

            var currentPage = Shell.Current?.CurrentPage as ContentPage;
            if (currentPage == null)
                return;

            bool shouldShowBanner = !_networkMonitor.IsNetworkAvailable;

            // Obtenir ou créer le bandeau pour cette page
            if (!_pageBanners.TryGetValue(currentPage, out var banner))
            {
                banner = CreateOfflineBannerView();
                _pageBanners[currentPage] = banner;
            }

            // Vérifier si la page a déjà un Grid comme contenu
            if (currentPage.Content is Grid grid)
            {
                ApplyToGrid(grid, banner, shouldShowBanner);
            }
            else if (shouldShowBanner && currentPage.Content != null)
            {
                WrapContentWithBanner(currentPage, banner);
            }
        }

        /// <summary>
        /// Crée une nouvelle vue du bandeau hors ligne
        /// </summary>
        private OfflineBanner CreateOfflineBannerView()
        {
            return new OfflineBanner();
        }

        /// <summary>
        /// Applique ou retire le bandeau d'un Grid existant
        /// </summary>
        private void ApplyToGrid(Grid grid, OfflineBanner banner, bool shouldShowBanner)
        {
            // Chercher si le bandeau existe déjà
            var existingBanner = grid.Children.FirstOrDefault(c => c == banner);

            if (shouldShowBanner && existingBanner == null)
            {
                // Vérifier si le Grid a déjà des RowDefinitions
                bool hasRowDefinitions = grid.RowDefinitions.Count > 0;

                if (hasRowDefinitions)
                {
                    // Ajouter une nouvelle ligne en haut
                    grid.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });

                    // Décaler tous les enfants existants d'une ligne
                    var childrenToUpdate = grid.Children.Where(c => c != banner).ToList();
                    foreach (var child in childrenToUpdate)
                    {
                        var currentRow = grid.GetRow(child);
                        grid.SetRow(child, currentRow + 1);
                    }
                }
                else
                {
                    // Pas de RowDefinitions, en créer
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Bandeau
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Contenu

                    // Mettre tous les enfants existants à la ligne 1
                    var childrenToUpdate = grid.Children.Where(c => c != banner).ToList();
                    foreach (var child in childrenToUpdate)
                    {
                        grid.SetRow(child, 1);
                    }
                }

                // Ajouter le bandeau à la ligne 0
                grid.SetRow(banner, 0);
                grid.Children.Insert(0, banner);
            }
            else if (!shouldShowBanner && existingBanner != null)
            {
                // Retirer le bandeau
                grid.Children.Remove(banner);

                // Vérifier s'il y a plus d'une RowDefinition avant de supprimer
                if (grid.RowDefinitions.Count > 0)
                {
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
        }

        /// <summary>
        /// Wrappe le contenu de la page avec un Grid contenant le bandeau
        /// </summary>
        private void WrapContentWithBanner(ContentPage page, OfflineBanner banner)
        {
            if (page.Content == null)
                return;

            var originalContent = page.Content;
            var wrapperGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto }, // Bandeau
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } // Contenu original
                }
            };

            wrapperGrid.SetRow(banner, 0);
            wrapperGrid.Children.Add(banner);

            wrapperGrid.SetRow(originalContent, 1);
            wrapperGrid.Children.Add(originalContent);

            page.Content = wrapperGrid;
        }

        /// <summary>
        /// Gestionnaire d'événement pour les changements de connectivité
        /// </summary>
        private void OnConnectivityChanged(object? sender, NetworkAccess access)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ApplyToCurrentPage();
            });
        }

        /// <summary>
        /// Nettoie les ressources
        /// </summary>
        public void Cleanup()
        {
            if (_networkMonitor != null)
            {
                _networkMonitor.ConnectivityChanged -= OnConnectivityChanged;
            }
            _pageBanners.Clear();
        }
    }
}
