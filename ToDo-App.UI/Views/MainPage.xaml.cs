using System.Diagnostics;
using Microsoft.UI.Xaml.Automation;
using System.Globalization;
using Microsoft.Windows.Storage.Pickers;
using ToDo_App.Controller;
using ToDo_App.Interfaces;
using ToDo_App.UI.Appearance;
using ToDo_App.UI.Controls;
using Windows.ApplicationModel.DataTransfer;
using Res = ToDo_App.UI.Resources;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using static ToDo_App.UI.Resources;

namespace ToDo_App.UI.Views
{
    public partial class MainPage : Page
    {
        private const string DefaultJsonPath = "Assets/default_board.json";
        private const string UserJsonPath = "board.json";
        private const double ScrollSpeed = 60;
        private const double ScrollTickSpeed = 10;

        private static readonly Dictionary<string, string> Languages = new()
        {
            {"en-US", "language/english"},
            {"da-DK", "language/danish"},
            {"de", "language/german"},
            {"fo", "language/faroese"},
            {"kln", "language/klingon"},
            {"naq", "language/navi"}
        };

        ITodoBoard boardController = new BoardController();

        List<Guid> columns = new List<Guid>();

        DispatcherTimer horizontalScrollTimer = new DispatcherTimer();
        double scrollDirection = 0;
        private const double VerticalZoneHeight = 80;
        DispatcherTimer verticalScrollTimer = new DispatcherTimer();
        double verticalScrollDirection = 0;
        ScrollViewer? verticalScrollTarget = null;

        
        CultureInfo _currentCulture;
        
        public MainPage()
        {
            InitializeComponent();

            boardController.ColumnCreated += onColumnCreated;
            boardController.ColumnNameChanged += onColumnNameChanged;
            boardController.ColumnRemoved += onColumnRemoved;
            boardController.ColumnContentChanged += onColumnContentChanged;
            boardController.BoardChanged += onBoardChanged;
            boardController.ColumnOrderChanged += onColumnOrderChanged;
            boardController.TagChanged += onTagChanged;
            BoardScrollViewer.DragOver += onBoardDragOver;
            BoardScrollViewer.AllowDrop = true;
            this.Content.AllowDrop = true;
            this.Content.DragOver += onBoardDragOver;
            this.Content.DragLeave += (s, e) => stopAutoScroll();
            horizontalScrollTimer.Interval = TimeSpan.FromMilliseconds(ScrollTickSpeed);
            horizontalScrollTimer.Tick += onHorizontalScrollTimerTick;
            BoardScrollViewer.DragLeave += (s, e) => stopAutoScroll();
            verticalScrollTimer.Interval = TimeSpan.FromMilliseconds(ScrollTickSpeed);
            verticalScrollTimer.Tick += onVerticalScrollTimerTick;

            this.Loaded += onPageInitialized;
        }

        private void onPageInitialized(object? sender, RoutedEventArgs eventArgs)
        {
            loadInitialBoard();
            initializeLanguageSettings();
            localizeXaml();
        }

        private async void loadInitialBoard()
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
                        Title = Res.ResourceManager.GetString("error/failed_to_load_board", _currentCulture),
                        Content = defaultFileException.Message,
                        CloseButtonText = Res.ResourceManager.GetString("dialog/ok", _currentCulture)
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
            if (columns.Count == 0)
            {
                return;
            }

            Guid firstColumn = columns[0];
            boardController.CreateNote(firstColumn,
                Res.ResourceManager.GetString("todo/default_title", _currentCulture),
                Res.ResourceManager.GetString("todo/default_description", _currentCulture));
        }

        private async void onImportBoardClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable the button to avoid double-clicking
                ImportButton.IsEnabled = false;
                
                var picker = new FileOpenPicker(ExportButton.XamlRoot.ContentIslandEnvironment.AppWindowId)
                {
                    CommitButtonText = Res.ResourceManager.GetString("dialog/import_board", _currentCulture),
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    FileTypeFilter = { ".json" }
                };
                
                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    // The controller raises ColumnRemoved for the current columns, so the UI clears itself
                    boardController.LoadBoardFromJson(file.Path);
                }
            }
            catch (Exception exception)
            {
                var dialog = new ContentDialog()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = Res.ResourceManager.GetString("error/failed_to_import_board", _currentCulture),
                    Content = exception.Message,
                    CloseButtonText = Res.ResourceManager.GetString("dialog/ok", _currentCulture)
                };

                await dialog.ShowAsync();
            }
            
            ImportButton.IsEnabled = true;
        }

        private async void onExportBoardClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable the button to avoid double-clicking
                ExportButton.IsEnabled = false;
                
                var picker = new FileSavePicker(ExportButton.XamlRoot.ContentIslandEnvironment.AppWindowId)
                {
                    CommitButtonText = Res.ResourceManager.GetString("dialog/export_board", _currentCulture),
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    FileTypeChoices = {{Res.ResourceManager.GetString("dialog/json_files", _currentCulture), new List<string> { ".json" }}}
                };
                
                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    boardController.SaveBoardToJson(file.Path);
                }
            }
            catch (Exception exception)
            {
                var dialog = new ContentDialog()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = Res.ResourceManager.GetString("error/failed_to_export_board", _currentCulture),
                    Content = exception.Message,
                    CloseButtonText = Res.ResourceManager.GetString("dialog/ok", _currentCulture)
                };

                await dialog.ShowAsync();
            }
            
            ExportButton.IsEnabled = true;
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

        private void onColumnRemoved(object? sender, EventArgs eventArgs)
        {
            if (eventArgs is not ColumnRemovedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }

            removeColumn(args.ColumnId);
            columns.Remove(args.ColumnId);

            foreach (Guid columnId in columns)
            {
                Border column = (Border)ColumnsPanel.FindName($"ROOT{columnId.ToString()}");
                if (column != null)
                {
                    Grid.SetColumn(column, columns.IndexOf(columnId));
                }
            }
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

        private void onTagChanged(object? sender, EventArgs eventArgs)
        {
            if (eventArgs is not TagChangedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }

            // Redraw every column with a card that shows the tag
            foreach (Guid columnId in columns)
            {
                if (boardController.GetNotesInColumn(columnId).Exists(note => note.Tags.Exists(tag => tag.Id == args.TagId)))
                {
                    clearColumnItems(columnId);
                    populateStatusColumn(columnId);
                }
            }
        }

        private void onColumnOrderChanged(object? sender, EventArgs e)
        {
            if (e is not ColumnOrderChangedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }

            columns = new List<Guid>(args.ColumnIds);

            for (int i = 0; i < columns.Count; i++)
            {
                string borderName = $"ROOT{columns[i]}";

                foreach (UIElement child in ColumnsPanel.Children)
                {
                    if (child is Border border && border.Name == borderName)
                    {
                        Grid.SetColumn(border, i);
                        break;
                    }
                }
            }
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
                    Title = Res.ResourceManager.GetString("error/failed_to_save_board", _currentCulture),
                    Content = e.Message,
                    CloseButtonText = Res.ResourceManager.GetString("dialog/ok", _currentCulture)
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
            column.AllowDrop = true;
            column.DragOver += onColumnDragOver;

            Grid columnGrid = new Grid { RowSpacing = 8 };
            columnGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            columnGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            columnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            columnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Small edit button
            Button editButton = new Button
            {
                Content = new SymbolIcon(Symbol.Edit),
                Padding = new Thickness(6),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(editButton, Res.ResourceManager.GetString("button/edit_column_title_tooltip", _currentCulture));
            editButton.Click += (sender, e) => onEditColumnClicked(sender, e, columnId);

            // Small delete button
            Button deleteButton = new Button
            {
                Content = new SymbolIcon(Symbol.Delete),
                Padding = new Thickness(6),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(deleteButton, Res.ResourceManager.GetString("button/delete_column_tooltip", _currentCulture));
            deleteButton.Click += (sender, e) => onDeleteColumnClicked(sender, e, columnId);

            // Regular add button
            Button addTodoToColumnButton = new Button
            {
                Content = new SymbolIcon(Symbol.Add),
                Style = (Style)Application.Current.Resources["PrimaryAction"],
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(addTodoToColumnButton, Res.ResourceManager.GetString("button/add_note_tooltip", _currentCulture));
            addTodoToColumnButton.Click += (sender, e) => onAddTodoToColumnClicked(sender, e, columnId);

            // Column name (also the drag handle for moving the column)
            TextBlock header = new TextBlock
            {
                Style = (Style)Resources["BoardColumnHeader"],
                Text = columnName,
                CanDrag = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.DragStarting += (s, e) => onColumn_DragStarting(columnId, e);
            header.DropCompleted += (s, e) => stopAutoScroll();

            // Stack the edit and delete buttons vertically
            StackPanel columnActions = new StackPanel
            {
                Spacing = 0,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { editButton, deleteButton }
            };

            // Left-hand section: stacked buttons and name
            Grid headerLeft = new Grid
            {
                ColumnSpacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                Children = { columnActions, header }
            };
            Grid.SetColumn(columnActions, 0);
            Grid.SetColumn(header, 1);

            var cardPanel = new StackPanel
            {
                Name = columnId.ToString(),
                Spacing = 8
            };
            column.Drop += (s, e) => onColumnDrop(columnId, cardPanel, e);

            ScrollViewer scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = cardPanel
            };
            scrollViewer.Background = new SolidColorBrush(Colors.Transparent);
            scrollViewer.AllowDrop = true;
            scrollViewer.DragOver += (s, e) => onColumnScrollDragOver(scrollViewer, e);
            scrollViewer.DragLeave += (s, e) => stopAutoScroll();

            Grid.SetColumn(addTodoToColumnButton, 1);
            Grid.SetRow(scrollViewer, 1);
            Grid.SetColumnSpan(scrollViewer, 2);

            columnGrid.Children.Add(headerLeft);
            columnGrid.Children.Add(addTodoToColumnButton);
            columnGrid.Children.Add(scrollViewer);

            column.Child = columnGrid;

            return column;
        }

        private void onColumnScrollDragOver(ScrollViewer column, DragEventArgs e)
        {
            if (!e.DataView.Properties.ContainsKey("noteId"))
            {
                return;
            }

            double y = e.GetPosition(column).Y;

            const double fastZone = VerticalZoneHeight/2;

            if (y < fastZone)
            {
                verticalScrollDirection = -ScrollSpeed * 2;
            }
            else if (y < VerticalZoneHeight && y >= fastZone)
            {
                verticalScrollDirection = -ScrollSpeed;
            }
            else if (y > column.ActualHeight - VerticalZoneHeight && y <= column.ActualHeight - fastZone)
            {
                verticalScrollDirection = ScrollSpeed;
            }
            else if (y > column.ActualHeight - fastZone)
            {
                verticalScrollDirection = ScrollSpeed * 2;
            }
            else
            {
                verticalScrollDirection = 0;
            }
            verticalScrollTarget = column;
            if (!verticalScrollTimer.IsEnabled)
            {
                verticalScrollTimer.Start();
            }
        }

        private void onColumnDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey("noteId") ||
                e.DataView.Properties.ContainsKey("columnId"))
            {
                e.AcceptedOperation = DataPackageOperation.Move;
            }
        }

        private void onBoardDragOver(object sender, DragEventArgs e)
        {

            double x = e.GetPosition(BoardScrollViewer).X;
            const double zoneWidth = 60;
            const double fastZone = 20;

            if (x < fastZone)
            {
                scrollDirection = -ScrollSpeed * 2;
            }
            else if (x < zoneWidth && x >= fastZone)
            {
                scrollDirection = -ScrollSpeed;
            }
            else if (x > BoardScrollViewer.ActualWidth - zoneWidth && x <= BoardScrollViewer.ActualWidth - fastZone)
            {
                scrollDirection = ScrollSpeed;
            }
            else if (x > BoardScrollViewer.ActualWidth - fastZone)
            {
                scrollDirection = ScrollSpeed*2;
            }
            else
            {
                scrollDirection = 0;
            }
            if (!horizontalScrollTimer.IsEnabled)
            {
                horizontalScrollTimer.Start();
            }
        }

        private void onHorizontalScrollTimerTick(object? sender, object e)
        {
            if (scrollDirection == 0)
            {
                horizontalScrollTimer.Stop();
                return;
            }
            BoardScrollViewer.ChangeView(BoardScrollViewer.HorizontalOffset + scrollDirection, null, null, false);
        }

        private void onVerticalScrollTimerTick(object? sender, object e)
        {
            if (verticalScrollDirection == 0 || verticalScrollTarget == null)
            {
                verticalScrollTimer.Stop();
                return;
            }

            verticalScrollTarget.ChangeView(null, verticalScrollTarget.VerticalOffset + verticalScrollDirection, null, false);
        }

        private void stopAutoScroll()
        {
            scrollDirection = 0;
            horizontalScrollTimer.Stop();
            verticalScrollDirection = 0;
            verticalScrollTarget = null;
            verticalScrollTimer.Stop();
        }

        private void onColumnDrop(Guid columnId, StackPanel panel, DragEventArgs e)
        {
            stopAutoScroll();
            if (e.DataView.Properties.TryGetValue("noteId", out object? noteValue) && noteValue is Guid noteId)
            {
                double y = e.GetPosition(panel).Y;
                int dropIndex = panel.Children.Count;
                for (int i = 0; i < panel.Children.Count; i++)
                {
                    UIElement card = panel.Children[i];
                    double midpoint = card.ActualOffset.Y + card.ActualSize.Y / 2;
                    if (y < midpoint)
                    {
                        dropIndex = i;
                        break;
                    }
                }
                List<ToDoNote> notes = boardController.GetNotesInColumn(columnId);
                int currentIndex = notes.FindIndex(n => n.Id == noteId);
                if (currentIndex != -1 && currentIndex < dropIndex)
                {
                    dropIndex--;
                }
                boardController.MoveNoteToColumn(noteId, columnId, dropIndex);
            }
            else if (e.DataView.Properties.TryGetValue("columnId", out object? columnValue) && columnValue is Guid draggedColumnId)
            {
                double x = e.GetPosition(ColumnsPanel).X;
                List<Guid> order = boardController.GetStatusColumnIds();
                int dropIndex = order.Count;

                foreach (UIElement child in ColumnsPanel.Children)
                {
                    if (child is Border border && border.Name.StartsWith("ROOT"))
                    {
                        int gridColumn = Grid.GetColumn(border);
                        double midpoint = border.ActualOffset.X + border.ActualSize.X / 2;

                        if (x < midpoint && gridColumn < dropIndex)
                        {
                            dropIndex = gridColumn;
                        }
                    }
                }

                int currentIndex = order.IndexOf(draggedColumnId);
                if (currentIndex != -1 && currentIndex < dropIndex)
                {
                    dropIndex--;
                }

                boardController.MoveColumn(draggedColumnId, dropIndex);
            }
        }

        private void AddColumnToUI(Border column, Guid columnId)
        {
            if (!columns.Contains(columnId))
            {
                columns.Add(columnId);
            }
            
            ColumnsPanel.Children.Add(column);
            Grid.SetColumn(column, columns.IndexOf(columnId));

            ColumnsPanel.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star),
                    MinWidth = (double)Resources["BoardColumnMinWidth"]
                }
            );


            recalculateColumnsPanelMinWidth();
            populateStatusColumn(columnId);
        }

        private void recalculateColumnsPanelMinWidth()
        {  //Used when columns are added or deleted as elements like note content needs a defined min width to work with
            double columnMinWidth = (double)Resources["BoardColumnMinWidth"], spacing = ColumnsPanel.ColumnSpacing;
            double newMinWidth = columns.Count * columnMinWidth + (columns.Count - 1) * spacing;
            if(newMinWidth < 0) { newMinWidth = 0; }
            ColumnsPanel.MinWidth = newMinWidth;
        }

        private async void onAddTodoToColumnClicked(object sender, RoutedEventArgs e, Guid columnId)
        {
            string defaultTitle = Res.ResourceManager.GetString("todo/default_title", _currentCulture);
            string defaultContent = Res.ResourceManager.GetString("todo/default_description", _currentCulture);

            // Placeholders (not prefilled text) so the user can start typing straight away
            var titleBox = new TextBox
            {
                Header = Res.ResourceManager.GetString("todo/property_title", _currentCulture),
                PlaceholderText = defaultTitle
            };

            var contentBox = new TextBox
            {
                Header = Res.ResourceManager.GetString("todo/property_description", _currentCulture),
                PlaceholderText = defaultContent,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100
            };

            var tagEditor = new TagEditor(boardController, Array.Empty<NoteTag>(), tagEditorStrings());

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = Res.ResourceManager.GetString("dialog/title_add_new_note", _currentCulture),
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/add", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                DefaultButton = ContentDialogButton.Primary,
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children = { titleBox, contentBox, tagEditor }
                }
            };
            // Keep the dialog open if a typed tag isn't valid, so the user can see why
            dialog.PrimaryButtonClick += (s, args) => args.Cancel = !tagEditor.CommitPendingInput();

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                // Leaving a field empty keeps the placeholder text, as it did when the fields were prefilled
                boardController.CreateNote(
                    columnId,
                    string.IsNullOrWhiteSpace(titleBox.Text) ? defaultTitle : titleBox.Text,
                    string.IsNullOrWhiteSpace(contentBox.Text) ? defaultContent : contentBox.Text,
                    tagNames: tagEditor.TagNames,
                    newTagColors: tagEditor.NewTagColors);
            }
        }

        private async void onEditColumnClicked(object sender, RoutedEventArgs e, Guid columnId)
        {
            TextBox nameBox = new TextBox
            {
                Text = boardController.GetStatusColumnName(columnId)
            };

            ContentDialog dialog = new ContentDialog
            {
                Title = Res.ResourceManager.GetString("dialog/title_edit_column", _currentCulture),
                Content = nameBox,
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/save", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                boardController.SetStatusColumnName(columnId, nameBox.Text);
            }
        }

        private async void onDeleteColumnClicked(object sender, RoutedEventArgs e, Guid columnId)
        {
            string columnName = boardController.GetStatusColumnName(columnId);
            int noteCount = boardController.GetNotesInColumn(columnId).Count;
            List<Guid> otherColumns = boardController.GetStatusColumnIds().FindAll(id => id != columnId);

            TextBlock message = new TextBlock
            {
                Text = String.Format(Res.ResourceManager.GetString("dialog/content_delete_column", _currentCulture), columnName),
                TextWrapping = TextWrapping.Wrap
            };

            // Only ask what to do with the ToDos when there are some, and somewhere to move them
            ComboBox? todoActionBox = null;
            if (noteCount > 0 && otherColumns.Count > 0)
            {
                todoActionBox = new ComboBox
                {
                    Header = String.Format(Res.ResourceManager.GetString("dialog/content_delete_column_consequence", _currentCulture), noteCount),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                foreach (Guid otherColumn in otherColumns)
                {
                    todoActionBox.Items.Add(new ComboBoxItem
                    {
                        Content = String.Format(Res.ResourceManager.GetString("dialog/option_move_to_column", _currentCulture), boardController.GetStatusColumnName(otherColumn)),
                        Tag = otherColumn
                    });
                }
                todoActionBox.Items.Add(new ComboBoxItem { Content = Res.ResourceManager.GetString("dialog/delete_the_todos", _currentCulture) });
                todoActionBox.SelectedIndex = 0;
            }
            else if (noteCount > 0)
            {
                message.Text = String.Format(Res.ResourceManager.GetString("dialog/content_delete_column_no_space", _currentCulture), columnName, noteCount);
            }

            StackPanel content = new StackPanel { Spacing = 12 };
            content.Children.Add(message);
            if (todoActionBox != null)
            {
                content.Children.Add(todoActionBox);
            }

            ContentDialog dialog = new ContentDialog
            {
                Title = Res.ResourceManager.GetString("dialog/title_delete_column_q", _currentCulture),
                Content = content,
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/delete", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (todoActionBox?.SelectedItem is ComboBoxItem { Tag: Guid moveToColumn })
                {
                    boardController.RemoveColumn(columnId, moveToColumn);
                }
                else
                {
                    boardController.RemoveColumn(columnId);
                }
                recalculateColumnsPanelMinWidth();
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

        private async void onDeleteTodoClicked(ToDoNote note)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = Res.ResourceManager.GetString("dialog/title_delete_note_q", _currentCulture),
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/delete", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                Content = String.Format(Res.ResourceManager.GetString("dialog/content_delete_note", _currentCulture), note.Title),
                DefaultButton = ContentDialogButton.Primary,
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                boardController.RemoveNote(note.Id);
            }
        }

        private async void onEditTodoClicked(ToDoNote note)
        {
            var titleBox = new TextBox
            {
                Header = Res.ResourceManager.GetString("todo/property_title", _currentCulture),
                Text = note.Title
            };

            var contentBox = new TextBox
            {
                Header = Res.ResourceManager.GetString("todo/property_description", _currentCulture),
                Text = note.Content,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100
            };
            
            var tagEditor = new TagEditor(boardController, note.Tags, tagEditorStrings());

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = Res.ResourceManager.GetString("dialog/title_edit_note", _currentCulture),
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/save", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                DefaultButton = ContentDialogButton.Primary,
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children = { titleBox, contentBox, tagEditor }
                }
            };
            // Keep the dialog open if a typed tag isn't valid, so the user can see why
            dialog.PrimaryButtonClick += (s, args) => args.Cancel = !tagEditor.CommitPendingInput();

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                boardController.SetNoteTitle(note.Id, titleBox.Text);
                boardController.SetNoteContent(note.Id, contentBox.Text);
                boardController.SetNoteTags(note.Id, tagEditor.TagNames, tagEditor.NewTagColors);
            }
        }

        private async void onAttachFileClicked(ToDoNote note, Button button)
        {
            try
            {
                // Disable the button to avoid double-clicking
                button.IsEnabled = false;
            
                var picker = new FileOpenPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId)
                {
                    CommitButtonText = Res.ResourceManager.GetString("dialog/attach_file", _currentCulture),
                    SuggestedStartLocation = PickerLocationId.Desktop
                };
            
                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    boardController.SetNoteFileAttachment(note.Id, file.Path);
                }
                else
                {
                    // Re-enable button if no file was selected
                    button.IsEnabled = true;
                }
            }
            catch (Exception e)
            {
                var dialog = new ContentDialog()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = Res.ResourceManager.GetString("error/failed_to_attach_file", _currentCulture),
                    Content = e.Message,
                    CloseButtonText = Res.ResourceManager.GetString("dialog/ok", _currentCulture)
                };

                await dialog.ShowAsync();
                
                button.IsEnabled = true;
            }
        }

        private async void onAttachmentClicked(ToDoNote note)
        {
            try
            {
                Process.Start("explorer", $"\"{note.File.FullPath}\"");
            }
            catch (Exception e)
            {
                var dialog = new ContentDialog()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = Res.ResourceManager.GetString("error/failed_to_open_attached_file", _currentCulture),
                    Content = e.Message,
                    CloseButtonText = Res.ResourceManager.GetString("dialog/ok", _currentCulture)
                };

                await dialog.ShowAsync();
            }
        }

        private async void onRemoveAttachmentClicked(ToDoNote note)
        {
            try
            {
                boardController.RemoveNoteFileAttachment(note.Id);
            }
            catch (Exception e)
            {
                var dialog = new ContentDialog()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = Res.ResourceManager.GetString("error/failed_to_remove_attached_file", _currentCulture),
                    Content = e.Message,
                    CloseButtonText = Res.ResourceManager.GetString("dialog/ok", _currentCulture)
                };

                await dialog.ShowAsync();
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

            var descriptionText = new TextBlock
            {
                Style = (Style)Resources["TodoDescription"],
                Text = note.Content,
                TextWrapping = TextWrapping.Wrap
            };
            
            var editButton = new Button
            {
                Content = new SymbolIcon(Symbol.Edit),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(editButton, Res.ResourceManager.GetString("button/edit_tooltip", _currentCulture));
            editButton.Click += (s, e) => onEditTodoClicked(note);
        
            var attachButton = new Button
            {
                Content = new SymbolIcon(Symbol.Attach),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            ToolTipService.SetToolTip(attachButton, Res.ResourceManager.GetString("button/attach_file_tooltip", _currentCulture));
            attachButton.Click += (s, e) => onAttachFileClicked(note, attachButton);
        
            var deleteButton = new Button
            {
                Content = new SymbolIcon(Symbol.Delete),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            ToolTipService.SetToolTip(deleteButton, Res.ResourceManager.GetString("button/delete_tooltip", _currentCulture));
            deleteButton.Click += (s, e) => onDeleteTodoClicked(note);

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
        
            var descriptionRow = new Grid
            {
                ColumnSpacing = 8,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children = { descriptionText, attachButton }
            };
            Grid.SetColumn(attachButton, 1);

            Grid attachmentRow;
            if (note.File != null)
            {
                attachButton.IsEnabled = false;
                
                var attachedFileButton = new Button
                {
                    Style = (Style)Application.Current.Resources["AttachedFile"],
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            new SymbolIcon(Symbol.Attach),
                            new TextBlock {Text = note.File.Name}
                        }
                    }
                };
                attachedFileButton.Click += (s, e) => onAttachmentClicked(note);
                
                var removeAttachmentButton = new Button
                {
                    Content = new SymbolIcon(Symbol.Cancel),
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                ToolTipService.SetToolTip(attachButton, Res.ResourceManager.GetString("button/remove_attached_file_tooltip", _currentCulture));
                removeAttachmentButton.Click += (s, e) => onRemoveAttachmentClicked(note);

                var attachmentButtonsStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    Children =
                    {
                        attachedFileButton,
                        removeAttachmentButton
                    }
                };
                
                attachmentRow = new Grid
                {
                    ColumnSpacing = 8,
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Children =  { attachmentButtonsStack, deleteButton }
                };
            }
            else
            {
                attachmentRow = new Grid
                {
                    ColumnSpacing = 8,
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Children =  { deleteButton }
                };
            }
            Grid.SetColumn(deleteButton, 1);

            var cardContent = new StackPanel { Spacing = 4 };
            cardContent.Children.Add(titleRow);
            if (note.Tags.Count > 0)
            {
                cardContent.Children.Add(buildTagsRow(note));
            }
            cardContent.Children.Add(descriptionRow);
            cardContent.Children.Add(attachmentRow);

            var card = new Border
            {
                Style = (Style)Resources["TodoCard"],
                CanDrag = true,
                Child = cardContent
            };
            card.DragStarting += (s, e) => onNote_DragStarting(note, e);
            card.DropCompleted += (s, e) => stopAutoScroll();
            return card;
        }

        private WrapPanel buildTagsRow(ToDoNote note)
        {
            var tagsRow = new WrapPanel();
            AutomationProperties.SetName(tagsRow, "Tags");

            foreach (NoteTag tag in note.Tags)
            {
                tagsRow.Children.Add(new Border
                {
                    Style = (Style)Resources["TodoTag"],
                    Background = new SolidColorBrush(AccentPalette.Find(tag.Color).Color),
                    Child = new TextBlock
                    {
                        Style = (Style)Resources["TodoTagText"],
                        Text = tag.Name
                    }
                });
            }

            return tagsRow;
        }

        private Dictionary<string, string> tagEditorStrings()
        {
            string[] keys =
            {
                "todo/property_tags", "dialog/add_tag_placeholder", "dialog/remove_tag", "dialog/edit_tag",
                "dialog/new_tag_tooltip", "dialog/tag_name", "dialog/tag_used_on", "dialog/tag_new_info",
                "dialog/tag_name_taken", "dialog/tag_colour", "dialog/rename", "dialog/delete", "dialog/cancel", "dialog/delete_tag_confirm"
            };
            // A missing translation shows the key instead of crashing the dialog
            return keys.ToDictionary(key => key, key => Res.ResourceManager.GetString(key, _currentCulture) ?? key);
        }

        private void onNote_DragStarting(ToDoNote note, DragStartingEventArgs args)
        {
            args.Data.Properties["noteId"] = note.Id;
            args.AllowedOperations = DataPackageOperation.Move;
        }

        private void onColumn_DragStarting(Guid columnId, DragStartingEventArgs args)
        {
            args.Data.Properties["columnId"] = columnId;
            args.AllowedOperations = DataPackageOperation.Move;
        }



        private async void OnClickAddColumn(object sender, RoutedEventArgs e)
        {  //Button to add new columns to the board
            TextBox columnNameBox = new TextBox
            {
                PlaceholderText = Res.ResourceManager.GetString("todo/column_title", _currentCulture)
            };

            ContentDialog dialog = new ContentDialog //Enter column name
            {
                Title = Res.ResourceManager.GetString("dialog/title_add_new_column", _currentCulture),
                Content = columnNameBox,
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/add", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
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

        private void initializeLanguageSettings()
        {
            foreach (var language in Languages)
            {
                LanguageSelector.Items.Add(new ComboBoxItem
                {
                    Content = Res.ResourceManager.GetString(language.Value, _currentCulture),
                    Tag = language.Key
                });
            }
            LanguageSelector.SelectedIndex = 0;
        }

        private void onLanguageSelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            var item = selectionChangedEventArgs.AddedItems[0] as ComboBoxItem;
            _currentCulture = new CultureInfo(item.Tag.ToString());
            localizeXaml();
        }
        
        private void localizeXaml()
        {
            AddColumnButton.Content = Res.ResourceManager.GetString("button/add_column", _currentCulture);
            AddToDoButton.Content = Res.ResourceManager.GetString("button/add_todo", _currentCulture);
            ImportButtonTextBlock.Text = Res.ResourceManager.GetString("button/import", _currentCulture);
            ExportButtonTextBlock.Text = Res.ResourceManager.GetString("button/export", _currentCulture);

            foreach (ComboBoxItem item in LanguageSelector.Items)
            {
                var languageStringName = Languages[item.Tag.ToString()];
                item.Content = Res.ResourceManager.GetString(languageStringName, _currentCulture);
            }
            
            AppearanceSelector.SetStrings(
                Res.ResourceManager.GetString("button/tooltip_light", _currentCulture),
                Res.ResourceManager.GetString("button/tooltip_system_default", _currentCulture),
                Res.ResourceManager.GetString("button/tooltip_dark", _currentCulture),
                Res.ResourceManager.GetString("button/tooltip_accent_color", _currentCulture)
                );
        }
    }
}
