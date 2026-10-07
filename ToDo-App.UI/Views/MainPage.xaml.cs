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
                var column = buildColumn(columnId, boardController.GetStatusColumnName(columnId));
                ColumnsPanel.Children.Add(column);
                Grid.SetColumn(column, columns.IndexOf(columnId));
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
            ColumnsPanel.Children.Add(column);
            
            columns.Add(args.ColumnId);
            Grid.SetColumn(column, columns.IndexOf(args.ColumnId));
        }

        private void onColumnNameChanged(object? sender, EventArgs eventArgs)
        {
            if (eventArgs is not ColumnNameChangedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }
            
            //TODO: update column text
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
                            Text = columnName,
                            Margin = new Thickness(0, 0, 0, 8)
                        },
                        scrollViewer
                    }
                }
            };

            return column;
            {
                createColumn(columnId);
            }
        }

        private void createColumn(Guid columnId)
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
                Content = "Edit",
                Style = (Style)Application.Current.Resources["PrimaryAction"],
                Margin = new Thickness(0, 0, 8, 0)
            };
            editButton.Click += (sender, e) => onEditColumnClicked(sender, e, columnId);
            columnGrid.Children.Add(editButton);

            TextBlock header = new TextBlock
            {
                Style = (Style)Resources["BoardColumnHeader"],
                Text = boardController.GetStatusColumnName(columnId),
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

            ColumnsPanel.Children.Add(column);
            Grid.SetColumn(column, columns.IndexOf(columnId));
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
                //ToDo replace with event handeling
                removeColumn(columnId);
                createColumn(columnId);
                populateStatus(columnId);
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
