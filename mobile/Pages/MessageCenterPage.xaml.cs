namespace mobile.Pages
{
    public partial class MessageCenterPage : ContentPage
    {
        public MessageCenterPage ()
        {
            InitializeComponent();
        }

        private async void OnBackgroundTapped (object sender, EventArgs e)
        {
            // Fermer la page modale
            await Navigation.PopModalAsync();
        }
    }
}
