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
        List<Guid>columns = new List<Guid>();
        public MainPage()
        {
            boardController.CreateColumn("Planned");
            boardController.CreateColumn("In Progress");
            boardController.CreateColumn("Completed");
            this.InitializeComponent();
            initializeColumns();
            foreach (Guid guid in columns)
            {
                populateStatus((guid));
            }

        }

        private void populateStatus(Guid column)
        {
            List<ToDoNote> items = boardController.GetNotesInColumn(column);
            StackPanel columnPanel = (StackPanel)ColumnsPanel.FindName(column.ToString());
            if (columnPanel != null)
            {
                foreach (ToDoNote item in items)
                {
                    Border card = buildToDoCard(item.Id.ToString(), item.Content);
                    columnPanel.Children.Add(card);
                }
            }
        }

        private void initializeColumns()
        {
            columns = boardController.GetStatusColumnIds();
            foreach (Guid columnId in columns)
            {

                var column = new Border();

                column.Style = (Style)Resources["BoardColumn"];

                Grid columnGrid = new Grid();
                columnGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                columnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                columnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                columnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });


                Button editButton = new Button
                {
                    Content = "Edit"
                };
                columnGrid.Children.Add(editButton);

                TextBlock header = new TextBlock
                {
                    Style = (Style)Resources["BoardColumnHeader"],
                    Text = boardController.GetStatusColumnName(columnId),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                Grid.SetColumn(header, 1);
                columnGrid.Children.Add(header);
                
                ScrollViewer scrollViewer = new ScrollViewer
                {
                    Content = new StackPanel
                    {
                        Name = columnId.ToString(),
                        Spacing = 8
                    }
                };
                Grid.SetRow(scrollViewer, 1);
                Grid.SetColumnSpan(scrollViewer, 2);
                columnGrid.Children.Add(scrollViewer);

                column.Child = columnGrid;

                ColumnsPanel.Children.Add(column);
                Grid.SetColumn(column, columns.IndexOf(columnId));
            }
        }

        // Stub: adds a placeholder card to "Planned" until the real models and "new ToDo" flow exist.
        private void OnAddTodoClicked(object sender, RoutedEventArgs e)
        {
            Guid firstColumn = columns[0];
            boardController.CreateNote(firstColumn, "New ToDo", "This is a new ToDo item.");

            //ToDo replace with event handeling
            clearColumnItems(firstColumn);
            populateStatus(firstColumn);
        }

        private void clearColumnItems(Guid column)
        {
            StackPanel columnPanel = (StackPanel)ColumnsPanel.FindName(column.ToString());
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
