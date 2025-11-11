using System.Windows.Input;

namespace mobile.Controls;

public partial class BaseIconButton : ContentView
{
    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(BaseIconButton), string.Empty,
            propertyChanged: OnGlyphChanged);

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(BaseIconButton), 18.0,
            propertyChanged: OnSizeChanged);

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(BaseIconButton), Colors.Black,
            propertyChanged: OnColorChanged);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(BaseIconButton), null,
            propertyChanged: OnCommandChanged);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(BaseIconButton), null);

    public static readonly BindableProperty ClickedCommandProperty =
        BindableProperty.Create(nameof(ClickedCommand), typeof(ICommand), typeof(BaseIconButton), null);

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public Color IconColor
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public ICommand ClickedCommand
    {
        get => (ICommand)GetValue(ClickedCommandProperty);
        set => SetValue(ClickedCommandProperty, value);
    }

    public event EventHandler<EventArgs>? Clicked;

    public BaseIconButton()
    {
        InitializeComponent();
        IconButton.Clicked += OnIconButtonClicked;
    }

    private static void OnGlyphChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (BaseIconButton)bindable;
        control.UpdateIcon();
    }

    private static void OnSizeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (BaseIconButton)bindable;
        control.UpdateIcon();
    }

    private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (BaseIconButton)bindable;
        control.UpdateIcon();
    }

    private static void OnCommandChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (BaseIconButton)bindable;
        control.IconButton.Command = (ICommand)newValue;
    }

    private void UpdateIcon()
    {
        if (string.IsNullOrEmpty(Glyph))
            return;

        IconButton.Source = new FontImageSource
        {
            Glyph = Glyph,
            FontFamily = "SegoeMDL2",
            Size = Size,
            Color = IconColor
        };
    }

    private void OnIconButtonClicked(object? sender, EventArgs e)
    {
        Clicked?.Invoke(this, e);
        ClickedCommand?.Execute(CommandParameter);
    }
}
