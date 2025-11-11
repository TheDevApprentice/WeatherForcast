using System.Windows.Input;

namespace mobile.Controls;

public partial class CircularBorderedIconButton : ContentView
{
    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(CircularBorderedIconButton), string.Empty,
            propertyChanged: OnGlyphChanged);

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(CircularBorderedIconButton), 20.0,
            propertyChanged: OnCornerRadiusChanged);

    public static readonly BindableProperty IconSizeProperty =
        BindableProperty.Create(nameof(IconSize), typeof(double), typeof(CircularBorderedIconButton), 18.0,
            propertyChanged: OnIconSizeChanged);

    public static readonly BindableProperty IconColorProperty =
        BindableProperty.Create(nameof(IconColor), typeof(Color), typeof(CircularBorderedIconButton), 
            Colors.Black, propertyChanged: OnIconColorChanged);

    public static readonly BindableProperty ClickedCommandProperty =
        BindableProperty.Create(nameof(ClickedCommand), typeof(ICommand), typeof(CircularBorderedIconButton), null);

    public static readonly BindableProperty PaddingValueProperty =
        BindableProperty.Create(nameof(PaddingValue), typeof(Thickness), typeof(CircularBorderedIconButton), 
            new Thickness(7));

    public static readonly BindableProperty MarginValueProperty =
        BindableProperty.Create(nameof(MarginValue), typeof(Thickness), typeof(CircularBorderedIconButton), 
            new Thickness(10, 0, 5, 0));

    public static readonly BindableProperty BackgroundColorProperty =
        BindableProperty.Create(nameof(BackgroundColor), typeof(Color), typeof(CircularBorderedIconButton), 
            null, propertyChanged: OnBackgroundColorChanged);

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }
    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }
    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public ICommand ClickedCommand
    {
        get => (ICommand)GetValue(ClickedCommandProperty);
        set => SetValue(ClickedCommandProperty, value);
    }

    public Thickness PaddingValue
    {
        get => (Thickness)GetValue(PaddingValueProperty);
        set => SetValue(PaddingValueProperty, value);
    }

    public Thickness MarginValue
    {
        get => (Thickness)GetValue(MarginValueProperty);
        set => SetValue(MarginValueProperty, value);
    }

    public new Color BackgroundColor
    {
        get => (Color)GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    public event EventHandler<EventArgs>? Clicked;

    public CircularBorderedIconButton()
    {
        InitializeComponent();
        IconButton.Clicked += OnIconButtonClicked;
        UpdateIcon();
    }

    private static void OnGlyphChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CircularBorderedIconButton)bindable;
        control.UpdateIcon();
    }
    private static void OnCornerRadiusChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CircularBorderedIconButton)bindable;
        control.UpdateIcon();
    }

    private static void OnBackgroundColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CircularBorderedIconButton)bindable;
        control.UpdateIcon();
    }
    private static void OnIconSizeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CircularBorderedIconButton)bindable;
        control.UpdateIcon();
    }

    private static void OnIconColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CircularBorderedIconButton)bindable;
        control.UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (string.IsNullOrEmpty(Glyph))
            return;

        IconButton.Source = new FontImageSource
        {
            Glyph = Glyph,
            FontFamily = "SegoeMDL2",
            Size = IconSize,
            Color = IconColor
        };
    }

    private void OnIconButtonClicked(object? sender, EventArgs e)
    {
        Clicked?.Invoke(this, e);
        ClickedCommand?.Execute(null);
    }
}
