namespace mobile.Controls
{
    /// <summary>
    /// Bouton flottant pour contacter le support
    /// Ouvre un chat popup style Messenger
    /// </summary>
    public partial class SupportButton : ContentView
    {
        public SupportButton()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Appelé quand on clique sur le bouton Support
        /// </summary>
        private async void OnSupportButtonTapped(object? sender, EventArgs e)
        {
            try
            {
                // Trouver le SupportChatPopup dans la page actuelle
                var currentPage = GetCurrentPage();
                if (currentPage == null) return;

                // Chercher le popup dans la page
                var popup = FindSupportChatPopup(currentPage);
                if (popup != null)
                {
                    await popup.ShowAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ SupportChatPopup non trouvé dans la page");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erreur lors de l'ouverture du chat support: {ex.Message}");
            }
        }

        private Page? GetCurrentPage()
        {
            if (Application.Current?.MainPage is Shell shell)
            {
                return shell.CurrentPage;
            }
            return Application.Current?.MainPage as Page;
        }

        private SupportChatPopup? FindSupportChatPopup(Element element)
        {
            // Chercher récursivement le SupportChatPopup
            if (element is SupportChatPopup popup)
                return popup;

            if (element is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is Element childElement)
                    {
                        var found = FindSupportChatPopup(childElement);
                        if (found != null) return found;
                    }
                }
            }
            else if (element is ContentPage page && page.Content is Element pageContent)
            {
                return FindSupportChatPopup(pageContent);
            }
            else if (element is ContentView view && view.Content is Element viewContent)
            {
                return FindSupportChatPopup(viewContent);
            }
            else if (element is ScrollView scroll && scroll.Content is Element scrollContent)
            {
                return FindSupportChatPopup(scrollContent);
            }

            return null;
        }
    }
}
