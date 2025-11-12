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
                    // SupportChatPopup non trouvé dans la page
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await Shell.Current.DisplayAlert("Debug SupportButton", $"❌ Erreur lors de l'ouverture du chat support: {ex.Message}\n{ex.GetType().Name}", "OK");
#endif
            }
        }

        private Page? GetCurrentPage()
        {
            if (Shell.Current != null)
            {
                return Shell.Current.CurrentPage;
            }

            // Fallback: current window's root page
            return this.Window?.Page as Page;
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
