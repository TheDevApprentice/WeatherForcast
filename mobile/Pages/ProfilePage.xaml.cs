using mobile.PageModels;
using Microsoft.Maui.Graphics;

namespace mobile.Pages
{
    public partial class ProfilePage : ContentPage
    {
        private CancellationTokenSource? _animCts;

        public ProfilePage(ProfilePageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            // Initialiser le switch selon le thème actuel
            ThemeSwitch.IsToggled = Application.Current?.UserAppTheme == AppTheme.Dark;

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

        private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

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
            _animCts?.Cancel();
            _animCts = new CancellationTokenSource();
            _ = StartRingRotationAsync(_animCts.Token);
            _ = StartHeaderGradientAnimationAsync(_animCts.Token);
        }

        private void OnScrollViewScrolled(object sender, ScrolledEventArgs e)
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

        private static double Clamp(double value, double min, double max)
            => value < min ? min : (value > max ? max : value);

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try { _animCts?.Cancel(); } catch { }
            this.AbortAnimation("HeaderGradientAnim");
        }

        private async Task StartRingRotationAsync(CancellationToken token)
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

        private Task StartHeaderGradientAnimationAsync(CancellationToken token)
        {
            if (HeaderGradStop1 == null || HeaderGradStop2 == null)
                return Task.CompletedTask;

            // Palettes Light / Dark
            var light1A = Color.FromArgb("#4F46E5");
            var light1B = Color.FromArgb("#06B6D4");
            var light2A = Color.FromArgb("#06B6D4");
            var light2B = Color.FromArgb("#22D3EE");

            var dark1A = Color.FromArgb("#0EA5E9");
            var dark1B = Color.FromArgb("#8B5CF6");
            var dark2A = Color.FromArgb("#8B5CF6");
            var dark2B = Color.FromArgb("#22D3EE");

            bool reverse = false;
            var animation = new Animation(t =>
            {
                var tt = reverse ? 1 - t : t;
                if (Application.Current?.RequestedTheme == AppTheme.Dark)
                {
                    HeaderGradStop1.Color = Lerp(dark1A, dark1B, tt);
                    HeaderGradStop2.Color = Lerp(dark2A, dark2B, tt);
                }
                else
                {
                    HeaderGradStop1.Color = Lerp(light1A, light1B, tt);
                    HeaderGradStop2.Color = Lerp(light2A, light2B, tt);
                }
            });

            void start()
            {
                this.AbortAnimation("HeaderGradientAnim");
                animation.Commit(this, "HeaderGradientAnim", rate: 16, length: 14000u, easing: Easing.Linear,
                    finished: (v, c) => { },
                    repeat: () =>
                    {
                        if (token.IsCancellationRequested) return false;
                        reverse = !reverse; // auto-reverse
                        return true;
                    });
            }

            start();
            return Task.CompletedTask;
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            t = Clamp(t, 0, 1);
            return Color.FromRgba(
                a.Red + (b.Red - a.Red) * t,
                a.Green + (b.Green - a.Green) * t,
                a.Blue + (b.Blue - a.Blue) * t,
                a.Alpha + (b.Alpha - a.Alpha) * t);
        }
    }
}
