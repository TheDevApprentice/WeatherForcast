namespace mobile.Controls
{
    public partial class FlyoutMenuItem : ContentView
    {
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(FlyoutMenuItem), string.Empty);

        public static readonly BindableProperty IconProperty =
            BindableProperty.Create(nameof(Icon), typeof(ImageSource), typeof(FlyoutMenuItem), null);

        public static readonly BindableProperty IsSelectedProperty =
            BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(FlyoutMenuItem), false,
                propertyChanged: OnIsSelectedChanged);

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public ImageSource Icon
        {
            get => (ImageSource)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public FlyoutMenuItem()
        {
            InitializeComponent();
        }

        private static void OnIsSelectedChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is FlyoutMenuItem menuItem && newValue is bool isSelected)
            {
                menuItem.UpdateSelectedState(isSelected);
            }
        }

        private void UpdateSelectedState(bool isSelected)
        {
            if (isSelected)
            {
                ItemCard.BackgroundColor = Color.FromArgb("#EEF0FF");
                ItemCard.Stroke = Color.FromArgb("#667eea");
                ItemCard.StrokeThickness = 2;
                MenuIcon.Opacity = 1;
                MenuTitle.FontAttributes = FontAttributes.Bold;
                MenuTitle.TextColor = Color.FromArgb("#667eea");
            }
            else
            {
                ItemCard.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                    ? Color.FromArgb("#1C1C1E") 
                    : Colors.White;
                ItemCard.Stroke = Colors.Transparent;
                ItemCard.StrokeThickness = 0;
                MenuIcon.Opacity = 0.85;
                MenuTitle.FontAttributes = FontAttributes.None;
                MenuTitle.TextColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                    ? Color.FromArgb("#F5F5F7") 
                    : Color.FromArgb("#1C1C1E");
            }
        }
    }
}
