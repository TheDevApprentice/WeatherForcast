using mobile.PageModels.Auth;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Controls;

namespace mobile.Pages.Auth
{
    public partial class RegisterPage : ContentPage
    {
        VerticalStackLayout? _firstNameGroup;
        VerticalStackLayout? _lastNameGroup;
        VerticalStackLayout? _passwordGroup;
        VerticalStackLayout? _confirmPasswordGroup;

        bool _isPasswordHidden = true;
        bool _isConfirmPasswordHidden = true;

        public RegisterPage (RegisterPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            SizeChanged += OnPageSizeChanged;

            // Récupère les références XAML (évite problèmes d'intellisense/lints)
            _firstNameGroup = this.FindByName<VerticalStackLayout>("FirstNameGroup");
            _lastNameGroup = this.FindByName<VerticalStackLayout>("LastNameGroup");
            _passwordGroup = this.FindByName<VerticalStackLayout>("PasswordGroup");
            _confirmPasswordGroup = this.FindByName<VerticalStackLayout>("ConfirmPasswordGroup");

            // Première application des règles
            UpdateResponsiveLayout(Width);

            // S'assurer que les champs sont masqués au démarrage
            SetPasswordVisibility(_isPasswordHidden, isConfirm: false);
            SetPasswordVisibility(_isConfirmPasswordHidden, isConfirm: true);
        }

        void OnPageSizeChanged (object? sender, EventArgs e)
        {
            UpdateResponsiveLayout(Width);
        }

        void UpdateResponsiveLayout (double width)
        {
            // Breakpoint simple: en dessous de 700px -> 1 colonne, sinon 2 colonnes
            const double breakpoint = 700;
            bool twoColumns = width >= breakpoint;

            // FlexBasis: second param 'isRelative' = true => pourcentage
            var basis50 = new FlexBasis(0.48f, true);
            var basis100 = new FlexBasis(1f, true);

            if (_firstNameGroup is not null) FlexLayout.SetBasis(_firstNameGroup, twoColumns ? basis50 : basis100);
            if (_lastNameGroup is not null) FlexLayout.SetBasis(_lastNameGroup, twoColumns ? basis50 : basis100);
            if (_passwordGroup is not null) FlexLayout.SetBasis(_passwordGroup, twoColumns ? basis50 : basis100);
            if (_confirmPasswordGroup is not null) FlexLayout.SetBasis(_confirmPasswordGroup, twoColumns ? basis50 : basis100);
        }

        void OnTogglePasswordClicked (object sender, EventArgs e)
        {
            // Retour haptique
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            _isPasswordHidden = !_isPasswordHidden;
            SetPasswordVisibility(_isPasswordHidden, isConfirm: false);
        }

        void OnToggleConfirmPasswordClicked (object sender, EventArgs e)
        {
            // Retour haptique
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            _isConfirmPasswordHidden = !_isConfirmPasswordHidden;
            SetPasswordVisibility(_isConfirmPasswordHidden, isConfirm: true);
        }

        void SetPasswordVisibility (bool hide, bool isConfirm)
        {
            var entry = this.FindByName<Entry>(isConfirm ? "ConfirmPasswordEntry" : "PasswordEntry");
            var button = this.FindByName<ImageButton>(isConfirm ? "ToggleConfirmPasswordButton" : "TogglePasswordButton");
            if (entry is null || button is null) return;

            entry.IsPassword = hide;

            // Icônes: œil (E890) quand masqué, 'hide' (ED1A) quand visible
            // IMPORTANT: utiliser Unicode C# (pas HTML entities)
            var glyph = hide ? "\uE890" : "\uED1A";
            
            // Récupérer la couleur depuis les ressources de manière sécurisée
            Color iconColor = Colors.Gray;
            if (Application.Current?.Resources.TryGetValue("Gray500", out var colorResource) == true && colorResource is Color color)
            {
                iconColor = color;
            }
            
            // Recréer complètement la source à chaque fois pour forcer le rafraîchissement
            button.Source = new FontImageSource
            {
                FontFamily = "SegoeMDL2",
                Glyph = glyph,
                Size = 18,
                Color = iconColor
            };
        }

        void OnNavigateToLoginClicked (object sender, EventArgs e)
        {
            // Retour haptique pour le lien de navigation
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
    }
}
