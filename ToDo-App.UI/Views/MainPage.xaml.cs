using ToDo_App;
using ToDo_App.Controller;
using ToDo_App.Interfaces;

namespace ToDo_App.UI.Views
{
    /// <summary>
    /// Kanban-style board mockup showing ToDos grouped into columns.
    /// </summary>
    public partial class MainPage : Page
    {
        ITodoBoard boardController = new BoardController();
        public MainPage()
        {
            this.InitializeComponent();
            boardController.CreateColumn("Planned");
            List<string> columnTitles = InitializeColumns();
            foreach (string title in columnTitles)
            {
                populateStatus(title);
            }

        }

        private void populateStatus(string title)
        {
            List<ToDoNote> items = boardController.GetNotesInColumn(title);
            StackPanel columnPanel = (StackPanel)ColumnsPanel.FindName($"{title.Replace(" ", "")}Items");
            if (columnPanel != null)
            {
                foreach (ToDoNote item in items)
                {
                    Border card = buildToDoCard(item.Id.ToString(), item.Content);
                    columnPanel.Children.Add(card);
                }
            }
        }

        private List<string> InitializeColumns()
        {
            List<string> columnTitles = boardController.GetStatusColumnNames();
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
                ColumnsPanel.Children.Add(column);
            }
            return columnTitles;
        }

        // Stub: adds a placeholder card to "Planned" until the real models and "new ToDo" flow exist.
        private void OnAddTodoClicked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException("Add ToDo flow not implemented yet.");
        }

        private Border buildToDoCard(string title, string description)
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
                            Text = title,
                        },
                        new TextBlock
                        {
                            Style = (Style)Resources["TodoDescription"],
                            Text = description,
                        }
                    }
                }
            };

            return card;
        }
    }
}
