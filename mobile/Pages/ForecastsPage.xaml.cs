using mobile.PageModels;

namespace mobile.Pages
{
    public partial class ForecastsPage : ContentPage
    {
        public ForecastsPage(ForecastsPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
