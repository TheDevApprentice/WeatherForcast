using mobile.PageModels.Auth;

namespace mobile.Pages.Auth
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
