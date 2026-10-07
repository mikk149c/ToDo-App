using ToDo_App;
using ToDo_App.Controller;
using ToDo_App.Interfaces;
using System.Diagnostics;

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
                    Border card = buildToDoCard(item.Title, item.Content);
                    columnPanel.Children.Add(card);
                }
            }
        }

        private void initializeColumns()
        {
            columns = boardController.GetStatusColumnIds();

            foreach (Guid columnId in columns)
            {
                AddColumnToUI(columnId);
            }
        }

        private void AddColumnToUI(Guid columnId)
        {
            ScrollViewer scrollViewer = new ScrollViewer
            {
                Content = new StackPanel
                {
                    Name = columnId.ToString(),
                    Spacing = 8
                }
            };

            Grid.SetRow(scrollViewer, 1);

            var column = new Border
            {
                Style = (Style)Resources["BoardColumn"],
                MinWidth = (double)Resources["BoardColumnMinWidth"],

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
                            Text = boardController.GetStatusColumnName(columnId),
                            Margin = new Thickness(0, 0, 0, 8)
                        },
                        scrollViewer
                    }
                }
            };

            ColumnsPanel.Children.Add(column);
            Grid.SetColumn(column, columns.IndexOf(columnId));

            populateStatus(columnId);
        }

        // Stub: adds a placeholder card to "Planned" until the real models and "new ToDo" flow exist.
        private void OnAddTodoClickedTest(object sender, RoutedEventArgs e)
        {
            Guid firstColumn = columns[0];
            boardController.CreateNote(firstColumn, "New ToDo", "This is a new ToDo item.");

            //ToDo replace with event handeling
            clearColumnItems(firstColumn);
            populateStatus(firstColumn);
        }

        private async void OnClickAddColumn(object sender, RoutedEventArgs e)
        {
            TextBox columnNameBox = new TextBox
            {
                PlaceholderText = "Column name"
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = "Create new column",
                Content = columnNameBox,
                PrimaryButtonText = "Done",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string columnName = columnNameBox.Text;

                Guid newColumn = boardController.CreateColumn(columnName);
                columns.Add(newColumn);

                ColumnsPanel.ColumnDefinitions.Add(
                    new ColumnDefinition{Width = new GridLength(1, GridUnitType.Star)}
                );
                AddColumnToUI(newColumn);
            }
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
