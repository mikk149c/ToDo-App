using ToDo_App.Controller;
using ToDo_App.Interfaces;

namespace ToDo_App.UI.Views
{
    public partial class MainPage : Page
    {
        private const string DefaultJsonPath = "Assets/default_board.json";
        private const string UserJsonPath = "board.json";
        
        ITodoBoard boardController = new BoardController();
        
        List<Guid> columns = new List<Guid>();
        
        public MainPage()
        {
            InitializeComponent();
            
            boardController.ColumnCreated += onColumnCreated;
            boardController.ColumnNameChanged += onColumnNameChanged;
            boardController.ColumnContentChanged += onColumnContentChanged;
            boardController.BoardChanged += onBoardChanged;
            
            this.Loaded += onPageInitialized;
        }

        private void onPageInitialized(object? sender, RoutedEventArgs eventArgs)
        {
            loadBoard();
        }

        private async void loadBoard()
        {
            try
            {
                boardController.LoadBoardFromJson(AppContext.BaseDirectory + UserJsonPath);
            }
            catch (Exception userFileException)
            {
                // If user board can't be loaded, load the default board instead
                try
                {
                    boardController.LoadBoardFromJson(AppContext.BaseDirectory + DefaultJsonPath);
                }
                catch (Exception defaultFileException)
                {
                    var dialog = new ContentDialog()
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Failed to load the board",
                        Content = defaultFileException.Message,
                        CloseButtonText = "Ok"
                    };

                    var result = await dialog.ShowAsync();
                }
            }
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
            AddColumnToUI(column, args.ColumnId);
        }

        private void onColumnNameChanged(object? sender, EventArgs eventArgs)
        {
            if (eventArgs is not ColumnNameChangedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }
            
            removeColumn(args.ColumnId);
            var column = buildColumn(args.ColumnId, args.NewName);
            AddColumnToUI(column, args.ColumnId);
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

        private async void onBoardChanged(object? sender, EventArgs eventArgs)
        {
            if (eventArgs is not BoardChangedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }

            try
            {
                boardController.SaveBoardToJson(AppContext.BaseDirectory + UserJsonPath);
            }
            catch (Exception e)
            {
                var dialog = new ContentDialog()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = "Failed to save the board",
                    Content = e.Message,
                    CloseButtonText = "Ok"
                };

                var result = await dialog.ShowAsync();
            }
        }

        private void clearColumnItems(Guid column)
        {
            StackPanel columnPanel = (StackPanel)ColumnsPanel.FindName(column.ToString());
            if (columnPanel != null){ columnPanel.Children.Clear(); }
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
        
        private void AddColumnToUI(Border column, Guid columnId)
        {
            columns.Add(columnId);
            
            ColumnsPanel.Children.Add(column);
            Grid.SetColumn(column, columns.IndexOf(columnId));

            ColumnsPanel.ColumnDefinitions.Add(
                new ColumnDefinition{Width = new GridLength(1, GridUnitType.Star)}
            );

            populateStatusColumn(columnId);
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
            
            ColumnsPanel.ColumnDefinitions.RemoveAt(0); //TODO: remove specific column instead of index 0
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

        private async void OnClickAddColumn(object sender, RoutedEventArgs e)
        {  //Button to add new columns to the board
            TextBox columnNameBox = new TextBox
            {
                PlaceholderText = "Column name"
            };

            ContentDialog dialog = new ContentDialog //Enter column name
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
            }
        }
    }
}
