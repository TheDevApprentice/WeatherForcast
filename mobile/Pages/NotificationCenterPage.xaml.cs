namespace mobile.Pages
{
    public partial class NotificationCenterPage : ContentPage
    {
        public NotificationCenterPage()
        {
            InitializeComponent();
        }

        private async void OnBackgroundTapped(object sender, EventArgs e)
        {
            // Fermer la page modale
            await Navigation.PopModalAsync();
        }
    }
}
