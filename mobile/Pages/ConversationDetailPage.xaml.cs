using Microsoft.Maui.Controls.Shapes;
using mobile.Controls;
using mobile.PageModels;
using mobile.Services.Stores;

namespace mobile.Pages
{
    /// <summary>
    /// Page affichant les messages d'une conversation et permettant d'envoyer des messages
    /// </summary>
    [QueryProperty(nameof(ConversationId), "conversationId")]
    public partial class ConversationDetailPage : ContentPage
    {
        private readonly ConversationDetailPageModel _viewModel;

        public string ConversationId
        {
            get => _viewModel.ConversationId;
            set => _viewModel.ConversationId = value;
        }

        public ConversationDetailPage(ConversationDetailPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;

            // S'abonner aux changements du ViewModel pour mettre à jour le header
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // S'abonner aux changements de la collection Messages
            _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.ConversationTitle) ||
                e.PropertyName == nameof(_viewModel.ConversationSubtitle))
            {
                UpdateHeader();
            }
        }

        private void OnMessagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateMessagesDisplay();
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
            UpdateHeader();
            UpdateMessagesDisplay();
        }

        private void UpdateHeader()
        {
            ConversationTitleLabel.Text = _viewModel.ConversationTitle;
            ConversationSubtitleLabel.Text = _viewModel.ConversationSubtitle;
        }

        private void UpdateMessagesDisplay()
        {
            // Vider la liste
            MessagesList.Children.Clear();

            // Afficher l'empty view
            EmptyView.IsVisible = _viewModel.IsEmpty;

            // Ajouter les messages
            foreach (var message in _viewModel.Messages)
            {
                var messageView = CreateMessageView(message);
                MessagesList.Children.Add(messageView);
            }

            // Scroller vers le bas
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                await MessagesScrollView.ScrollToAsync(0, MessagesList.Height, false);
            });
        }

        private View CreateMessageView (Message message)
        {
            // Déterminer si c'est un message de l'utilisateur ou du support
            bool isFromCurrentUser = message.Type != MessageType.Info &&
                                     message.Type != MessageType.Support;

            var border = new Border
            {
                BackgroundColor = isFromCurrentUser
                    ? Color.FromArgb("#3B82F6")
                    : Application.Current?.Resources["CardBackgroundColor"] as Color,
                Padding = new Thickness(12, 8),
                Margin = new Thickness(
                    isFromCurrentUser ? 60 : 0,
                    0,
                    isFromCurrentUser ? 0 : 60,
                    0
                ),
                StrokeThickness = isFromCurrentUser ? 0 : 1,
                Stroke = isFromCurrentUser ? null : Application.Current?.Resources["BorderColor"] as Brush,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(
                        isFromCurrentUser ? 16 : 4,
                        isFromCurrentUser ? 4 : 16,
                        16,
                        16
                    )
                },
                HorizontalOptions = isFromCurrentUser ? LayoutOptions.End : LayoutOptions.Start
            };

            var stackLayout = new VerticalStackLayout
            {
                Spacing = 4
            };

            // Titre si présent
            if (!string.IsNullOrEmpty(message.Title) && message.Title != "Message")
            {
                stackLayout.Children.Add(new Label
                {
                    Text = message.Title,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Application.Current?.Resources["PrimaryTextColor"] as Color
                });
            }

            // Contenu du message
            stackLayout.Children.Add(new Label
            {
                Text = message.Content,
                FontSize = 14,
                TextColor = Application.Current?.Resources["PrimaryTextColor"] as Color,
                LineBreakMode = LineBreakMode.WordWrap
            });

            // Timestamp
            stackLayout.Children.Add(new Label
            {
                Text = ConversationDetailPageModel.FormatTimestamp(message.Timestamp),
                FontSize = 11,
                TextColor = Application.Current?.Resources["TertiaryTextColor"] as Color,
                Opacity = 0.8,
                HorizontalOptions = LayoutOptions.End
            });

            border.Content = stackLayout;
            return border;
        }

    }
}
