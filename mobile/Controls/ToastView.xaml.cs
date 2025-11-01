namespace mobile.Controls
{
    /// <summary>
    /// Toast personnalisé avec animations
    /// </summary>
    public partial class ToastView : Frame
    {
        private CancellationTokenSource? _cancellationTokenSource;

        public ToastView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Affiche un toast avec animation
        /// </summary>
        public async Task ShowAsync(string message, ToastType type, int durationMs = 3000, bool showCloseButton = false)
        {
            // Annuler l'affichage précédent si en cours
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            // Configurer l'apparence selon le type
            ConfigureAppearance(type);

            // Configurer le message
            MessageLabel.Text = message;
            CloseButton.IsVisible = showCloseButton;

            // Animation d'entrée
            await AnimateInAsync();

            try
            {
                // Attendre la durée spécifiée
                await Task.Delay(durationMs, _cancellationTokenSource.Token);

                // Animation de sortie
                await AnimateOutAsync();
            }
            catch (TaskCanceledException)
            {
                // Toast annulé (nouveau toast affiché)
                await AnimateOutAsync();
            }
        }

        /// <summary>
        /// Configure l'apparence selon le type de toast
        /// </summary>
        private void ConfigureAppearance(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success:
                    BackgroundColor = Color.FromArgb("#10B981"); // Vert
                    IconLabel.Text = "✓";
                    break;

                case ToastType.Error:
                    BackgroundColor = Color.FromArgb("#EF4444"); // Rouge
                    IconLabel.Text = "✕";
                    break;

                case ToastType.Warning:
                    BackgroundColor = Color.FromArgb("#F59E0B"); // Orange
                    IconLabel.Text = "⚠";
                    break;

                case ToastType.Info:
                    BackgroundColor = Color.FromArgb("#3B82F6"); // Bleu
                    IconLabel.Text = "ℹ";
                    break;

                case ToastType.Default:
                default:
                    BackgroundColor = Color.FromArgb("#2D3748"); // Gris foncé
                    IconLabel.Text = "•";
                    break;
            }
        }

        /// <summary>
        /// Animation d'entrée (slide up + fade in)
        /// </summary>
        private async Task AnimateInAsync()
        {
            IsVisible = true;
            Opacity = 0;
            TranslationY = 50;

            await Task.WhenAll(
                this.FadeTo(1, 300, Easing.CubicOut),
                this.TranslateTo(0, 0, 300, Easing.CubicOut)
            );
        }

        /// <summary>
        /// Animation de sortie (fade out)
        /// </summary>
        private async Task AnimateOutAsync()
        {
            await this.FadeTo(0, 200, Easing.CubicIn);
            IsVisible = false;
        }

        /// <summary>
        /// Fermer le toast manuellement
        /// </summary>
        private void OnCloseTapped(object? sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }
    }

    /// <summary>
    /// Types de toast
    /// </summary>
    public enum ToastType
    {
        Default,
        Success,
        Error,
        Warning,
        Info
    }
}
