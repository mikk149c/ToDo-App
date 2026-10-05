namespace ToDo_App.UI.Views
{
    /// <summary>
    /// Kanban-style board mockup showing ToDos grouped into columns.
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            List<string> columnTitles = InitializeColumns();
            

                ColumnsPanel.Children.Add(column);
            }
        }

        private List<string> InitializeColumns()
        {
            
            foreach (string title in columnTitles)
            {
                var column = new Border
                {
                    Style = (Style)Resources["BoardColumn"],
                    Child = new Grid
                    {
                        RowDefinitions =
                        {
                            new RowDefinition { Height = GridLength.Auto },
                            new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                        },
                        Children =
                        {
                            new TextBlock
                            {
                                Style = (Style)Resources["ColumnHeader"],
                                Text = title,
                                Margin = new Thickness(0, 0, 0, 8)
                            },
                            new ScrollViewer
                            {
                                Content = new StackPanel
                                {
                                    Name = $"{title.Replace(" ", "")}Items",
                                    Spacing = 8
                                }
                            }
                        }
                    }
                };
        }

        // Stub: adds a placeholder card to "Planned" until the real models and "new ToDo" flow exist.
        private void OnAddTodoClicked(object sender, RoutedEventArgs e)
        {
            var card = new Border
            {
                Style = (Style)Resources["TodoCard"],
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock
                        {
                            Style = (Style)Resources["TodoTitle"],
                            Text = $"New ToDo {PlannedItems.Children.Count + 1}",
                        },
                        new TextBlock
                        {
                            Style = (Style)Resources["TodoDescription"],
                            Text = "This is a placeholder description for the new ToDo item.",
                        }
                    }
                }
            };

            PlannedItems.Children.Add(card);
        }
    }
}
