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
        List<(Guid, String)>columnTitles = new List<(Guid, String)>();
        public MainPage()
        {
            boardController.CreateColumn("Planned");
            boardController.CreateColumn("In Progress");
            boardController.CreateColumn("Completed");
            this.InitializeComponent();
            InitializeColumns();
            foreach ((Guid, String) title in columnTitles)
            {
                populateStatus(title);
            }

        }

        private void populateStatus((Guid, String) column)
        {
            List<ToDoNote> items = boardController.GetNotesInColumn(column.Item1);
            StackPanel columnPanel = (StackPanel)ColumnsPanel.FindName($"{column.Item2.Replace(" ", "")}Items");
            if (columnPanel != null)
            {
                foreach (ToDoNote item in items)
                {
                    Border card = buildToDoCard(item.Id.ToString(), item.Content);
                    columnPanel.Children.Add(card);
                }
            }
        }

        private void InitializeColumns()
        {
            columnTitles = boardController.GetStatusColumnIdsandNames();
            foreach ((Guid, String) title in columnTitles)
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
                                Style = (Style)Resources["BoardColumnHeader"],
                                Text = title.Item2,
                                Margin = new Thickness(0, 0, 0, 8)
                            },
                            new ScrollViewer
                            {
                                Content = new StackPanel
                                {
                                    Name = $"{title.Item2.Replace(" ", "")}Items",
                                    Spacing = 8
                                }
                            }
                        }
                    }
                };
                ColumnsPanel.Children.Add(column);
                Grid.SetColumn(column, columnTitles.IndexOf(title));
            }
        }

        // Stub: adds a placeholder card to "Planned" until the real models and "new ToDo" flow exist.
        private void OnAddTodoClicked(object sender, RoutedEventArgs e)
        {
            (Guid, String) firstColumn = columnTitles[0];
            Guid columnId = firstColumn.Item1;
            boardController.CreateNote(columnId, "New ToDo", "This is a new ToDo item.");
            clearColumnItems(firstColumn);
            populateStatus(firstColumn);
        }

        private void clearColumnItems((Guid, string) firstColumn)
        {
            StackPanel columnPanel = (StackPanel)ColumnsPanel.FindName($"{firstColumn.Item2.Replace(" ", "")}Items");
            if (columnPanel != null)
            {
                columnPanel.Children.Clear();
            }
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
