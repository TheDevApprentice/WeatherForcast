namespace mobile.Controls
{
    /// <summary>
    /// Conteneur pour afficher plusieurs toasts empilés
    /// </summary>
    public partial class ToastContainer : AbsoluteLayout
    {
        private static ToastContainer? _instance;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public ToastContainer()
        {
            InitializeComponent();
            _instance = this;
        }

        /// <summary>
        /// Obtient l'instance du conteneur de toasts
        /// </summary>
        public static ToastContainer? Instance => _instance;

        /// <summary>
        /// Affiche un toast
        /// </summary>
        public async Task ShowToastAsync(string message, ToastType type, int durationMs = 3000)
        {
            await _semaphore.WaitAsync();

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    System.Diagnostics.Debug.WriteLine($"🍞 ToastContainer: Création d'un toast - Message: {message}, Type: {type}");
                    
                    // Créer un nouveau toast
                    var toast = new ToastView();
                    
                    System.Diagnostics.Debug.WriteLine($"🍞 ToastContainer: Toast créé, ajout au stack (count: {ToastStack.Children.Count})");
                    
                    // Ajouter au stack
                    ToastStack.Children.Add(toast);

                    System.Diagnostics.Debug.WriteLine($"🍞 ToastContainer: Toast ajouté (count: {ToastStack.Children.Count}), affichage en cours...");

                    // Afficher avec animation
                    await toast.ShowAsync(message, type, durationMs);

                    System.Diagnostics.Debug.WriteLine($"🍞 ToastContainer: Toast affiché, retrait du stack...");

                    // Retirer du stack après l'animation
                    ToastStack.Children.Remove(toast);
                    
                    System.Diagnostics.Debug.WriteLine($"🍞 ToastContainer: Toast retiré (count: {ToastStack.Children.Count})");
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
