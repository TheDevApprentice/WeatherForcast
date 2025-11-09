using mobile.Services.Theme;

namespace mobile.Pages
{
    public partial class ProfilePage : ContentPage
    {
        private CancellationTokenSource? _animCts;
        private bool _isPageActive = false;
        private readonly IThemeService _themeService;

        public ProfilePage (ProfilePageModel viewModel, IThemeService themeService)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _themeService = themeService;

            // S'abonner aux changements de thème via le service centralisé
            _themeService.ThemeChanged += OnThemeChangedFromService;

            // Initialiser le switch selon le thème actuel
            ThemeSwitch.IsToggled = _themeService.CurrentTheme == AppTheme.Dark;

            // Etats init pour animation d'apparition
            if (HeaderContent != null)
            {
                HeaderContent.Opacity = 0;
                HeaderContent.TranslationY = 10;
            }
            if (ContentStack != null)
            {
                ContentStack.Opacity = 0;
                ContentStack.TranslationY = 10;
            }
        }

        private async void ThemeSwitch_Toggled (object sender, ToggledEventArgs e)
        {
            // Retour haptique sur toggle
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            // Animation subtile du switch
            if (sender is Switch switchControl)
            {
                await switchControl.ScaleTo(1.05, 100, Easing.CubicOut);
                await switchControl.ScaleTo(1.0, 100, Easing.CubicOut);
            }

            // Utiliser le service centralisé pour changer le thème
            await _themeService.SetThemeAsync(e.Value ? AppTheme.Dark : AppTheme.Light, animated: true);
        }

        protected override async void OnAppearing ()
        {
            base.OnAppearing();
            _isPageActive = true;

            // Animation d'apparition douce
            var tasks = new List<Task>();
            if (HeaderContent != null)
            {
                tasks.Add(HeaderContent.FadeTo(1, 300, Easing.CubicOut));
                tasks.Add(HeaderContent.TranslateTo(HeaderContent.TranslationX, 0, 300, Easing.CubicOut));
            }
            if (ContentStack != null)
            {
                // démarrage léger différé pour un effet de cascade
                await Task.Delay(60);
                tasks.Add(ContentStack.FadeTo(1, 300, Easing.CubicOut));
                tasks.Add(ContentStack.TranslateTo(ContentStack.TranslationX, 0, 300, Easing.CubicOut));
            }
            await Task.WhenAll(tasks);

            // Lancer animations lentes (ring + header gradient)
            StartAnimations();
        }

        private void OnScrollViewScrolled (object sender, ScrolledEventArgs e)
        {
            double y = e.ScrollY;

            // Elastic stretch on pull-to-stretch (overscroll at top)
            if (y <= 0)
            {
                double pull = -y;
                if (HeaderBackground != null)
                {
                    double stretchY = 1 + Math.Min(pull / 600.0, 0.12); // max +12%
                    HeaderBackground.ScaleY = stretchY;
                    HeaderBackground.TranslationY = -pull * 0.15; // keep gradient attached to the top
                }
                if (AvatarContainer != null)
                {
                    AvatarContainer.TranslationY = pull * 0.25;
                    AvatarContainer.Scale = 1 + Math.Min(pull / 900.0, 0.08); // subtle
                }
            }
            else
            {
                // Reset elastic transforms when scrolling down
                if (HeaderBackground != null)
                {
                    HeaderBackground.ScaleY = 1;
                    HeaderBackground.TranslationY = 0;
                }
                if (AvatarContainer != null)
                {
                    AvatarContainer.TranslationY = 0;
                    AvatarContainer.Scale = 1;
                }
            }

            // Parallax du header
            if (HeaderGrid != null)
            {
                HeaderGrid.TranslationY = -y * 0.18; // subtil
            }

            // Avatar scale + translation
            if (AvatarBorder != null)
            {
                double scale = 1 - (y / 700); // diminue doucement
                scale = Clamp(scale, 0.88, 1.0);
                AvatarBorder.Scale = scale;
                AvatarBorder.TranslationY = -y * 0.06;
            }

            // Texte: légère atténuation et translation
            if (NameLabel != null && EmailLabel != null)
            {
                double fade = 1 - (y / 800);
                fade = Clamp(fade, 0.7, 1.0);
                NameLabel.Opacity = fade;
                EmailLabel.Opacity = fade * 0.9;

                double ty = -y * 0.05;
                NameLabel.TranslationY = ty;
                EmailLabel.TranslationY = ty;
            }
        }

        private static double Clamp (double value, double min, double max)
            => value < min ? min : (value > max ? max : value);

        protected override void OnDisappearing ()
        {
            base.OnDisappearing();
            _isPageActive = false;
            StopAnimations();
        }

        /// <summary>
        /// Démarre les animations continues (ring et gradient)
        /// </summary>
        private void StartAnimations ()
        {
            if (!_isPageActive) return;

            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
            _ = StartRingRotationAsync(_animCts.Token);
            // Animation du gradient désactivée
        }

        /// <summary>
        /// Arrête toutes les animations continues
        /// </summary>
        private void StopAnimations ()
        {
            try
            {
                _animCts?.Cancel();
            }
            catch { /* Ignore cancellation errors */ }

            // this.AbortAnimation("HeaderGradientAnim");
        }

        /// <summary>
        /// Gère le changement de thème via le service centralisé
        /// </summary>
        private void OnThemeChangedFromService (object? sender, AppTheme theme)
        {
            // Redémarrer l'animation du gradient avec les nouvelles couleurs
            if (_isPageActive)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Animation du gradient désactivée
                });
            }
        }

        private async Task StartRingRotationAsync (CancellationToken token)
        {
            if (AvatarRing == null) return;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // rotation continue très lente
                    await AvatarRing.RotateTo(360, 12000u, Easing.Linear);
                    AvatarRing.Rotation = 0;
                }
            }
            catch { /* ignore on cancel */ }
        }

        // Animation du gradient désactivée complètement
        private Task StartHeaderGradientAnimationAsync (CancellationToken token)
        {
            return Task.CompletedTask;
        }

        // Animations UX pour les interactions
        private async void OnSettingsCardTapped (object sender, TappedEventArgs e)
        {
            // Retour haptique
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            if (sender is Border border)
            {
                // Animation de scale
                await border.ScaleTo(0.97, 100, Easing.CubicOut);
                await border.ScaleTo(1.0, 100, Easing.CubicOut);
            }

            // La commande OpenSettingsCommand du ViewModel gère l'ouverture du modal
        }

        async void OnButtonPressed (object sender, EventArgs e)
        {
            // Retour haptique
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            if (sender is Button button)
            {
                await button.ScaleTo(0.95, 100, Easing.CubicOut);
            }
        }

        async void OnButtonReleased (object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                await button.ScaleTo(1.0, 100, Easing.CubicOut);
            }
        }
    }
}
