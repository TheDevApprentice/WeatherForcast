using mobile.Controls;
using mobile.PageModels;
using mobile.Services.Stores;

namespace mobile.Pages
{
    /// <summary>
    /// Page affichant toutes les conversations de l'utilisateur
    /// </summary>
    public partial class ConversationsPage : ContentPage
    {
        private readonly ConversationsPageModel _viewModel;

        public ConversationsPage(ConversationsPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
            
            // S'abonner aux changements du ViewModel
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConversationsPageModel.FilteredConversations) ||
                e.PropertyName == nameof(ConversationsPageModel.IsEmpty))
            {
                UpdateConversationsDisplay();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
            UpdateConversationsDisplay();
        }

        private void UpdateConversationsDisplay()
        {
            // Vider la liste
            ConversationsList.Children.Clear();

            // Afficher l'empty view
            EmptyView.IsVisible = _viewModel.IsEmpty;

            // Ajouter les cartes de conversation
            foreach (var conversation in _viewModel.FilteredConversations)
            {
                var card = new ConversationCard();
                card.Initialize(conversation, "current-user"); // TODO: Utiliser l'ID utilisateur du ViewModel
                ConversationsList.Children.Add(card);
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.SearchText = e.NewTextValue ?? string.Empty;
            UpdateConversationsDisplay();
        }

        private async void OnNewConversationClicked(object sender, EventArgs e)
        {
            await _viewModel.CreateNewConversationCommand.ExecuteAsync(null);
        }
    }
}
