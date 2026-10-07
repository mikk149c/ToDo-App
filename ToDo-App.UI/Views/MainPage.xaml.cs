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
                    Border card = buildToDoCard(item, column);
                    columnPanel.Children.Add(card);
                }
            }
        }

        private void initializeColumns()
        {
            columns = boardController.GetStatusColumnIds();
            foreach (Guid columnId in columns)
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

        private async void onDeleteTodoClicked(ToDoNote note, Guid columnId)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Delete Note?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                Content = $"Delete \"{note.Title}\"? This can't be undone.",
                DefaultButton = ContentDialogButton.Primary,
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                boardController.RemoveNote(note.Id);
                clearColumnItems(columnId);
                populateStatus(columnId);
            }
        }

        private async void onEditTodoClicked(ToDoNote note, Guid columnId)
        {
            var titleBox = new TextBox
            {
                Header = "Title",
                Text = note.Title
            };

            var contentBox = new TextBox
            {
                Header = "Description",
                Text = note.Content,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100
            };

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Edit note",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children = { titleBox, contentBox }
                }
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                boardController.SetNoteTitle(note.Id, titleBox.Text);
                boardController.SetNoteContent(note.Id, contentBox.Text);

                clearColumnItems(columnId);
                populateStatus(columnId);
            }
        }

        private Border buildToDoCard(ToDoNote note, Guid columnId)
        {
            var titleText = new TextBlock
            {
                Style = (Style)Resources["TodoTitle"],
                Text = note.Title,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var editButton = new Button
            {
                Content = new SymbolIcon(Symbol.Edit),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(editButton, "Edit");
            editButton.Click += (s, e) => onEditTodoClicked(note, columnId);

            var titleRow = new Grid
            {
                ColumnSpacing = 8,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children = { titleText, editButton }
            };
            Grid.SetColumn(editButton, 1);

            var descriptionText = new TextBlock
            {
                Style = (Style)Resources["TodoDescription"],
                Text = note.Content,
                TextWrapping = TextWrapping.Wrap
            };
        
            var deleteButton = new Button
            {
                Content = new SymbolIcon(Symbol.Delete),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            ToolTipService.SetToolTip(deleteButton, "Delete");
            deleteButton.Click += (s, e) => onDeleteTodoClicked(note, columnId);
        
            var descriptionRow = new Grid
            {
                ColumnSpacing = 8,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children = { descriptionText, deleteButton }
            };
            Grid.SetColumn(deleteButton, 1);

            var card = new Border
            {
                Style = (Style)Resources["TodoCard"],
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        titleRow,
                        descriptionRow
                    }
                }
            };

            return card;
        }
    }
}
