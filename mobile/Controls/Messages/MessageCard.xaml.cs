namespace mobile.Controls
{
    /// <summary>
    /// Carte de message individuelle avec timer et animations
    /// </summary>
    public partial class MessageCard : ContentView
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isClosing = false;
        public event EventHandler? Closed;

        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        public MessageCard ()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Affiche le message avec animation et timer
        /// </summary>
        public async Task ShowAsync (string title, string content, MessageType type, int durationMs = 5000)
        {
            // Configurer l'apparence
            ConfigureAppearance(title, content, type);

            // Animation d'entrée
            await AnimateInAsync();

            // Démarrer le timer avec barre de progression
            _cancellationTokenSource = new CancellationTokenSource();
            _ = StartTimerAsync(durationMs, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Configure l'apparence selon le type
        /// </summary>
        private void ConfigureAppearance (string title, string content, MessageType type)
        {
            TitleLabel.Text = title;
            MessageLabel.Text = content;

            switch (type)
            {
                //case MessageType.Success:
                //    ColorBar.BackgroundColor = Color.FromArgb("#10B981"); // Vert
                //    TitleLabel.TextColor = Color.FromArgb("#059669");
                //    break;

                //case MessageType.Error:
                //    ColorBar.BackgroundColor = Color.FromArgb("#EF4444"); // Rouge
                //    TitleLabel.TextColor = Color.FromArgb("#DC2626");
                //    break;

                //case MessageType.Warning:
                //    ColorBar.BackgroundColor = Color.FromArgb("#F59E0B"); // Orange
                //    TitleLabel.TextColor = Color.FromArgb("#D97706");
                //    break;

                case MessageType.Info:
                    ColorBar.BackgroundColor = Color.FromArgb("#3B82F6"); // Bleu
                    TitleLabel.TextColor = Color.FromArgb("#2563EB");
                    break;
            }
        }

        /// <summary>
        /// Animation d'entrée (slide from right + fade in)
        /// </summary>
        private async Task AnimateInAsync ()
        {
            IsVisible = true;
            Opacity = 0;
            TranslationX = 400;

            await Task.WhenAll(
                this.FadeTo(1, 400, Easing.CubicOut),
                this.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }

        /// <summary>
        /// Animation de sortie (slide to right + fade out)
        /// </summary>
        private async Task AnimateOutAsync ()
        {
            if (_isClosing) return;
            _isClosing = true;

            await Task.WhenAll(
                this.FadeTo(0, 300, Easing.CubicIn),
                this.TranslateTo(400, 0, 300, Easing.CubicIn)
            );

            IsVisible = false;
            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Démarre le timer avec barre de progression
        /// </summary>
        private async Task StartTimerAsync (int durationMs, CancellationToken cancellationToken)
        {
            try
            {
                var startTime = DateTime.Now;
                var endTime = startTime.AddMilliseconds(durationMs);

                while (DateTime.Now < endTime && !cancellationToken.IsCancellationRequested)
                {
                    var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                    var progress = elapsed / durationMs;

                    // Mettre à jour la barre de progression
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (ProgressBar.Width > 0)
                        {
                            var width = ProgressBar.Width * progress;
                            ProgressClip.Rect = new Rect(0, 0, width, 2);
                        }
                    });

                    await Task.Delay(16, cancellationToken); // ~60 FPS
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await AnimateOutAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Timer annulé (fermeture manuelle)
            }
        }

        /// <summary>
        /// Fermer manuellement le message
        /// </summary>
        private void OnCloseTapped (object? sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _ = AnimateOutAsync();
        }

        /// <summary>
        /// Nettoyer les ressources
        /// </summary>
        public void Dispose ()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }

    /// <summary>
    /// Types de messages
    /// </summary>
    public enum MessageType
    {
        Info,
        User,      // Message envoyé par l'utilisateur
        Support    // Message envoyé par le support
    }
}
