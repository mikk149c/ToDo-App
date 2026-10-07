using ToDo_App.Controller;
using ToDo_App.Interfaces;

namespace ToDo_App.UI.Views
{
    public partial class MainPage : Page
    {
        ITodoBoard boardController = new BoardController();
        
        List<Guid> columns = new List<Guid>();
        
        public MainPage()
        {
            InitializeComponent();
            
            boardController.ColumnCreated += onColumnCreated;
            boardController.ColumnNameChanged += onColumnNameChanged;
            boardController.ColumnContentChanged += onColumnContentChanged;
            
            //TODO: load from json
            boardController.CreateColumn("Planned");
            boardController.CreateColumn("In Progress");
            boardController.CreateColumn("Completed");
        }

        private void populateStatusColumn(Guid column)
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

        private void OnAddTodoClicked(object sender, RoutedEventArgs e)
        {
            Guid firstColumn = columns[0];
            boardController.CreateNote(firstColumn, "New ToDo", "This is a new ToDo item.");
        }

        private void onColumnCreated(object? sender, EventArgs eventArgs)
        {
            if (eventArgs is not ColumnCreatedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }

            var column = buildColumn(args.ColumnId, args.Name);
            columns.Add(args.ColumnId);
            Grid.SetColumn(column, columns.IndexOf(args.ColumnId));
            ColumnsPanel.Children.Add(column);
        }

        private void onColumnNameChanged(object? sender, EventArgs eventArgs)
        {
            if (eventArgs is not ColumnNameChangedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }
            
            removeColumn(args.ColumnId);
            var column = buildColumn(args.ColumnId, args.NewName);
            Grid.SetColumn(column, columns.IndexOf(args.ColumnId));
            ColumnsPanel.Children.Add(column);
            populateStatusColumn(args.ColumnId);
        }

        private void onColumnContentChanged(object? sender, EventArgs eventArgs)
        {
            if (eventArgs is not ColumnContentChangedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }
            
            clearColumnItems(args.ColumnId);
            populateStatusColumn(args.ColumnId);
        }

        private void clearColumnItems(Guid column)
        {
            StackPanel columnPanel = (StackPanel)ColumnsPanel.FindName(column.ToString());
            if (columnPanel != null)
            {
                columnPanel.Children.Clear();
            }
        }

        private Border buildColumn(Guid columnId, string columnName)
        {
            var column = new Border();
            
            column.Name = $"ROOT{columnId.ToString()}";
            column.Style = (Style)Resources["BoardColumn"];

            Grid columnGrid = new Grid { RowSpacing = 8 };
            columnGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            columnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            columnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            columnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });


            Button editButton = new Button
            {
                Content = new SymbolIcon(Symbol.Edit),
                Style = (Style)Application.Current.Resources["PrimaryAction"],
                Margin = new Thickness(0, 0, 8, 0)
            };
            editButton.Click += (sender, e) => onEditColumnClicked(sender, e, columnId);
            columnGrid.Children.Add(editButton);

            TextBlock header = new TextBlock
            {
                Style = (Style)Resources["BoardColumnHeader"],
                Text = columnName,
                VerticalAlignment = VerticalAlignment.Center
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

            return column;
        }

        private async void onEditColumnClicked(object sender, RoutedEventArgs e, Guid columnId)
        {
            TextBox nameBox = new TextBox
            {
                Text = boardController.GetStatusColumnName(columnId)
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = "Edit column",
                Content = nameBox,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                boardController.SetStatusColumnName(columnId, nameBox.Text);
            }
        }

        private void removeColumn(Guid columnId)
        {
            Border columnToRemove = (Border)ColumnsPanel.FindName($"ROOT{columnId.ToString()}");
            if (columnToRemove != null)
            {
                ColumnsPanel.Children.Remove(columnToRemove);
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
