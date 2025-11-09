using mobile.Models;
using Microsoft.Maui.Controls.Shapes;

namespace mobile.Pages
{
    /// <summary>
    /// Page affichant toutes les conversations de l'utilisateur
    /// </summary>
    public partial class ConversationsPage : ContentPage
    {
        private readonly IConversationStore _conversationStore;
        private string _currentUserId = "current-user"; // TODO: Récupérer l'ID utilisateur réel
        private List<Conversation> _allConversations = new();

        public ConversationsPage()
        {
            InitializeComponent();

            // Récupérer le store
            _conversationStore = Application.Current?.Handler?.MauiContext?.Services.GetService<IConversationStore>()
                ?? throw new InvalidOperationException("IConversationStore not found");

            // S'abonner aux changements
            _conversationStore.Conversations.CollectionChanged += OnConversationsChanged;

            // Charger les conversations
            LoadConversations();
        }

        private void OnConversationsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            LoadConversations();
        }

        private void LoadConversations()
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                _allConversations = _conversationStore.Conversations.ToList();
                UpdateConversationsDisplay(_allConversations);
            });
        }

        private void UpdateConversationsDisplay(List<Conversation> conversations)
        {
            // Vider la liste
            ConversationsList.Children.Clear();

            // Afficher l'empty view si aucune conversation
            EmptyView.IsVisible = conversations.Count == 0;

            // Ajouter les cartes de conversation
            foreach (var conversation in conversations)
            {
                var card = CreateConversationCard(conversation);
                ConversationsList.Children.Add(card);
            }
        }

        private Border CreateConversationCard(Conversation conversation)
        {
            var card = new Border
            {
                BackgroundColor = conversation.HasUnreadMessages 
                    ? Application.Current?.Resources["SelectedItemBackground"] as Color 
                    : Application.Current?.Resources["CardBackgroundColor"] as Color,
                Padding = new Thickness(16, 12),
                Margin = new Thickness(0, 0, 0, 1),
                StrokeThickness = 0
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await OnConversationTapped(conversation);
            card.GestureRecognizers.Add(tapGesture);

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnSpacing = 12,
                RowSpacing = 4
            };

            // Avatar
            var avatar = new Border
            {
                WidthRequest = 56,
                HeightRequest = 56,
                BackgroundColor = Application.Current?.Resources["AccentColor"] as Color,
                StrokeShape = new RoundRectangle { CornerRadius = 28 },
                StrokeThickness = 0,
                VerticalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = conversation.GetInitials(_currentUserId),
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            Grid.SetColumn(avatar, 0);
            Grid.SetRowSpan(avatar, 2);
            grid.Children.Add(avatar);

            // Badge épinglé
            if (conversation.IsPinned)
            {
                var pinnedBadge = new Border
                {
                    WidthRequest = 20,
                    HeightRequest = 20,
                    BackgroundColor = Application.Current?.Resources["CardBackgroundColor"] as Color,
                    StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    StrokeThickness = 2,
                    Stroke = Application.Current?.Resources["AccentColor"] as Brush,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Start,
                    Margin = new Thickness(0, -4, -4, 0),
                    Content = new Label
                    {
                        Text = "📌",
                        FontSize = 11,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };
                Grid.SetColumn(pinnedBadge, 0);
                grid.Children.Add(pinnedBadge);
            }

            // Nom
            var nameLabel = new Label
            {
                Text = conversation.GetDisplayName(_currentUserId),
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Application.Current?.Resources["PrimaryTextColor"] as Color,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            };
            Grid.SetColumn(nameLabel, 1);
            Grid.SetRow(nameLabel, 0);
            grid.Children.Add(nameLabel);

            // Dernier message
            var lastMessageLabel = new Label
            {
                Text = conversation.LastMessage?.Content ?? "Aucun message",
                FontSize = 14,
                TextColor = Application.Current?.Resources["SecondaryTextColor"] as Color,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1
            };
            Grid.SetColumn(lastMessageLabel, 1);
            Grid.SetRow(lastMessageLabel, 1);
            grid.Children.Add(lastMessageLabel);

            // Timestamp
            if (conversation.LastMessage != null)
            {
                var timestampLabel = new Label
                {
                    Text = FormatTimestamp(conversation.LastMessage.Timestamp),
                    FontSize = 12,
                    TextColor = Application.Current?.Resources["TertiaryTextColor"] as Color,
                    Opacity = 0.7,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Start
                };
                Grid.SetColumn(timestampLabel, 2);
                Grid.SetRow(timestampLabel, 0);
                grid.Children.Add(timestampLabel);
            }

            // Badge non lu
            if (conversation.HasUnreadMessages)
            {
                var unreadBadge = new Border
                {
                    MinimumWidthRequest = 22,
                    HeightRequest = 22,
                    Padding = new Thickness(6, 2),
                    BackgroundColor = Color.FromArgb("#3B82F6"),
                    StrokeShape = new RoundRectangle { CornerRadius = 11 },
                    StrokeThickness = 0,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center,
                    Content = new Label
                    {
                        Text = conversation.UnreadCount > 99 ? "99+" : conversation.UnreadCount.ToString(),
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };
                Grid.SetColumn(unreadBadge, 2);
                Grid.SetRow(unreadBadge, 1);
                grid.Children.Add(unreadBadge);
            }

            card.Content = grid;
            return card;
        }

        private string FormatTimestamp(DateTime timestamp)
        {
            var now = DateTime.Now;
            var diff = now - timestamp;

            if (diff.TotalMinutes < 1)
                return "À l'instant";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}h";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}j";

            return timestamp.ToString("dd/MM/yyyy");
        }

        private async Task OnConversationTapped(Conversation conversation)
        {
            // Naviguer vers la page de détail de la conversation
            await Shell.Current.GoToAsync($"conversations/detail?conversationId={conversation.Id}");
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                UpdateConversationsDisplay(_allConversations);
                return;
            }

            var filtered = _allConversations.Where(c =>
                c.GetDisplayName(_currentUserId).ToLower().Contains(searchText) ||
                (c.LastMessage?.Content.ToLower().Contains(searchText) ?? false)
            ).ToList();

            UpdateConversationsDisplay(filtered);
        }

        private async void OnNewConversationClicked(object sender, EventArgs e)
        {
            // TODO: Implémenter la création d'une nouvelle conversation
            await DisplayAlert("Nouvelle conversation", "Fonctionnalité à venir", "OK");
        }
    }
}
