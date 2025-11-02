namespace mobile
{
    public partial class AppShellMobile : Shell
    {
        public AppShellMobile()
        {
            InitializeComponent();

            // Les routes sont gérées par navigation directe avec Shell.Current.Navigation.PushAsync
            // au lieu de Routing.RegisterRoute car les pages utilisent l'injection de dépendances
        }
    }
}
