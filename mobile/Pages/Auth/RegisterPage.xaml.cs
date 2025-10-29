using mobile.PageModels.Auth;

namespace mobile.Pages.Auth
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage(RegisterPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
