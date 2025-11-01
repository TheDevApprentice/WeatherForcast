using mobile.PageModels;

namespace mobile.Pages
{
    public partial class ForecastsPage : ContentPage
    {
        private readonly ForecastsPageModel _viewModel;

        public ForecastsPage(ForecastsPageModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // S'abonner aux événements SignalR quand on arrive sur la page
            _viewModel?.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Désabonner les événements SignalR quand on quitte la page
            _viewModel?.OnDisappearing();
        }
    }
}
