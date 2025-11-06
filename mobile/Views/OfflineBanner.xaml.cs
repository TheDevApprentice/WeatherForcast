namespace mobile.Views
{
    public partial class OfflineBanner : ContentView
    {
        public static readonly BindableProperty IsOnlineProperty =
            BindableProperty.Create(nameof(IsOnline), typeof(bool), typeof(OfflineBanner), false, propertyChanged: OnIsOnlineChanged);

        public bool IsOnline
        {
            get => (bool)GetValue(IsOnlineProperty);
            set => SetValue(IsOnlineProperty, value);
        }

        public OfflineBanner()
        {
            InitializeComponent();
        }

        private static void OnIsOnlineChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is OfflineBanner banner)
            {
                banner.UpdateBannerState();
            }
        }

        private void UpdateBannerState()
        {
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

            if (IsOnline)
            {
                // État "Connecté" - Vert
                BannerBorder.Background = isDark 
                    ? Color.FromArgb("#2E7D32") // Vert foncé pour dark mode
                    : Color.FromArgb("#4CAF50"); // Vert vif pour light mode
                
                IconLabel.Text = "✓";
                MessageLabel.Text = "Vous êtes à nouveau connecté !";
            }
            else
            {
                // État "Hors ligne" - Jaune/Orange
                BannerBorder.Background = isDark
                    ? Color.FromArgb("#D4A017") // Jaune ambré pour dark mode
                    : Color.FromArgb("#FFC107"); // Jaune vif pour light mode
                
                IconLabel.Text = "⚠️";
                MessageLabel.Text = "Vous êtes hors ligne";
            }
        }
    }
}
