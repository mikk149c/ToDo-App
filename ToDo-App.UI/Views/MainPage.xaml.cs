using System.Diagnostics;
using Microsoft.Windows.Storage.Pickers;
using ToDo_App.Controller;
using ToDo_App.Interfaces;
using System;

namespace ToDo_App.UI.Views
{
    public partial class MainPage : Page
    {
        private const string DefaultJsonPath = "Assets/default_board.json";
        private const string UserJsonPath = "board.json";
        private Guid selectedTrashNoteId = Guid.Empty;
        
        ITodoBoard boardController = new BoardController();
        
        List<Guid> columns = new List<Guid>();
        
        public MainPage()
        {
            InitializeComponent();
            
            boardController.ColumnCreated += onColumnCreated;
            boardController.ColumnNameChanged += onColumnNameChanged;
            boardController.ColumnRemoved += onColumnRemoved;
            boardController.ColumnContentChanged += onColumnContentChanged;
            boardController.BoardChanged += onBoardChanged;
            
            this.Loaded += onPageInitialized;
        }

        private void onPageInitialized(object? sender, RoutedEventArgs eventArgs)
        {
            loadInitialBoard();
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

        private void RefreshTrashBin(
            StackPanel listPanel,
            TextBlock confirmationText,
            Grid trashListView,
            StackPanel restorePanel,
            TextBlock restoreHeading,
            StackPanel restoreColumnButtons,
            StackPanel confirmationPanel)
        {
            listPanel.Children.Clear();

            foreach (ToDoNote note in boardController.GetTrashBinNotes())
            {       
                TextBlock title = new TextBlock
                {
                    Style = (Style)Resources["TodoTitle"],
                    Text = note.Title,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock expandIcon = new TextBlock { Text = ">", VerticalAlignment = VerticalAlignment.Center };

                Grid titleRow = new Grid
                {
                    ColumnSpacing = 8,
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    },
                    Children = { expandIcon, title }
                };

                Grid.SetColumn(title, 1);

                TextBlock description = new TextBlock
                {
                    Style = (Style)Resources["TodoDescription"],
                    Text = note.Content,
                    TextWrapping = TextWrapping.Wrap
                };

                StackPanel expandedContent = new StackPanel
                {
                    Spacing = 4,
                    Visibility = Visibility.Collapsed,
                    Children = { description }
                };

                Button titleButton = new Button
                {
                    Content = titleRow,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };

                titleButton.Click += (sender, e) =>
                {
                    bool isExpanded = expandedContent.Visibility == Visibility.Visible;

                    expandedContent.Visibility = isExpanded
                        ? Visibility.Collapsed
                        : Visibility.Visible;

                    expandIcon.Text = isExpanded ? ">" : "v";
                };

                StackPanel buttons = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Button restoreButton = new Button{ Content = new SymbolIcon(Symbol.Undo) };
                restoreButton.Click += (sender, e) =>
                {
                    selectedTrashNoteId = note.Id;
                    restoreHeading.Text = $"Select which column to restore \"{note.Title}\" to.";
                    restoreColumnButtons.Children.Clear();
                    List<Guid> columnIds = boardController.GetStatusColumnIds();

                    foreach (Guid columnId in columnIds)
                    {
                        string columnName = boardController.GetStatusColumnName(columnId);

                        TextBlock columnNameText = new TextBlock
                        {
                            Style = (Style)Application.Current.Resources["MyLabel"],
                            Text = $"{columnName}",
                            TextAlignment = TextAlignment.Left,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };

                        Button destinationButton = new Button
                        {
                            Content = columnNameText,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch
                        };

                        destinationButton.Click += (columnSender, columnE) =>
                        {
                            if (selectedTrashNoteId == Guid.Empty) { return; }
                            boardController.RestoreNote( selectedTrashNoteId, columnId);
                            selectedTrashNoteId = Guid.Empty;

                            RefreshTrashBin(listPanel, confirmationText, trashListView, restorePanel,
                                            restoreHeading, restoreColumnButtons, confirmationPanel);

                            restorePanel.Visibility = Visibility.Collapsed;
                            trashListView.Visibility = Visibility.Visible;
                        };

                        restoreColumnButtons.Children.Add(destinationButton);
                    }

                    restorePanel.Visibility = Visibility.Visible;
                    trashListView.Visibility = Visibility.Collapsed;
                };

                Button deleteButton = new Button { Content = new SymbolIcon(Symbol.Delete) };
                deleteButton.Click += (sender, e) => { 
                    selectedTrashNoteId = note.Id;
                    confirmationText.Text =
                    $"Are you sure you want to permanently delete \"{note.Title}\"?"; 
                    confirmationPanel.Visibility = Visibility.Visible;
                    trashListView.Visibility = Visibility.Collapsed;
                };

                buttons.Children.Add(restoreButton);
                buttons.Children.Add(deleteButton);

                // Put the clickable title and action buttons side by side.
                Grid headerRow = new Grid
                {
                    ColumnSpacing = 8,
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Children = { titleButton, buttons }
                };

                Grid.SetColumn(buttons, 1);

                // Assemble the card.
                Border card = new Border
                {
                    Style = (Style)Resources["TodoCard"],
                    Child = new StackPanel
                    {
                        Spacing = 4,
                        Children = { headerRow, expandedContent }
                    }
                };

                listPanel.Children.Add(card);
            }
        }

        private async void OnTrashBinClicked(object sender, RoutedEventArgs e)
        {
            StackPanel listPanel = new StackPanel { Spacing = 8 };

            Grid trashListView = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                },
                RowSpacing = 12,
                MaxHeight = 550
            };

            ScrollViewer trashScrollViewer = new ScrollViewer
            {
                Content = listPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 500
            };

            //Restore note section
            StackPanel restorePanel = new StackPanel
            {
                Spacing = 12,
                Visibility = Visibility.Collapsed
            };

            TextBlock restoreHeading = new TextBlock { TextWrapping = TextWrapping.Wrap };

            StackPanel restoreColumnButtons = new StackPanel { Spacing = 8 };

            ScrollViewer restoreColumnScrollViewer = new ScrollViewer
            {
                Content = restoreColumnButtons,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 400
            };

            Button cancelRestoreButton = new Button
            {
                Content = "Cancel",
                HorizontalAlignment = HorizontalAlignment.Right
            };

            cancelRestoreButton.Click += (sender, e) =>
            {
                selectedTrashNoteId = Guid.Empty;
                restorePanel.Visibility = Visibility.Collapsed;
                trashListView.Visibility = Visibility.Visible;
            };

            restorePanel.Children.Add(restoreHeading);
            restorePanel.Children.Add(restoreColumnScrollViewer);
            restorePanel.Children.Add(cancelRestoreButton);

            //Confirm delete section
            StackPanel confirmationPanel = new StackPanel
            {
                Spacing = 12,
                Visibility = Visibility.Collapsed
            };

            TextBlock confirmationText = new TextBlock
            {
                Text = "Are you sure you want to permanently delete this note?",
                TextWrapping = TextWrapping.Wrap
            };

            StackPanel confirmationButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8
            };

            Button cancelDeleteFromTrashButton = new Button { Content = "Cancel" };
            cancelDeleteFromTrashButton.Click += (sender, e) =>
            {
                selectedTrashNoteId = Guid.Empty;
                confirmationPanel.Visibility = Visibility.Collapsed;
                trashListView.Visibility = Visibility.Visible;
            };

            Button confirmDeleteFromTrashButton = new Button
            {
                Content = "Delete",
                Style = (Style)Application.Current.Resources["PrimaryAction"]
            };
            confirmDeleteFromTrashButton.Click += (sender, e) =>
            {
                OnClickConfirmDeleteFromTrashButton(sender, e, selectedTrashNoteId);
                RefreshTrashBin(listPanel, confirmationText,trashListView, restorePanel, 
                                restoreHeading, restoreColumnButtons, confirmationPanel);
                selectedTrashNoteId = Guid.Empty;
                confirmationPanel.Visibility = Visibility.Collapsed;
                trashListView.Visibility = Visibility.Visible;
            };

            confirmationButtons.Children.Add(cancelDeleteFromTrashButton);
            confirmationButtons.Children.Add(confirmDeleteFromTrashButton);

            confirmationPanel.Children.Add(confirmationText);
            confirmationPanel.Children.Add(confirmationButtons);

            //Build the trash bin
            Button closeTrashBinButton = new Button
            {
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            Grid.SetRow(trashScrollViewer, 0);
            Grid.SetRow(closeTrashBinButton, 1);
            trashListView.Children.Add(trashScrollViewer);
            trashListView.Children.Add(closeTrashBinButton);

            Grid dialogContent = new Grid { Width = 500 };
            dialogContent.Children.Add(trashListView);
            dialogContent.Children.Add(restorePanel);
            dialogContent.Children.Add(confirmationPanel);

            RefreshTrashBin(listPanel, confirmationText,trashListView, restorePanel, 
                            restoreHeading, restoreColumnButtons, confirmationPanel);

            ContentDialog dialog = new ContentDialog
            {
                Title = "Trash bin",
                Content = dialogContent,
                XamlRoot = this.XamlRoot
            };
            closeTrashBinButton.Click += (sender, e) => { dialog.Hide(); };

            await dialog.ShowAsync();
        }

        private void OnAddTodoClicked(object sender, RoutedEventArgs e)
        {
            if (columns.Count == 0)
            {
                return;
            }

            Guid firstColumn = columns[0];
            boardController.CreateNote(firstColumn, "New ToDo", "This is a new ToDo item.");
        }

        private void OnClickConfirmDeleteFromTrashButton(object sender, RoutedEventArgs e, Guid noteId)
        {
            boardController.PermanentlyRemoveNote(noteId);
        }

        private async void onImportBoardClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable the button to avoid double-clicking
                ImportButton.IsEnabled = false;
                
                var picker = new FileOpenPicker(ExportButton.XamlRoot.ContentIslandEnvironment.AppWindowId)
                {
                    CommitButtonText = "Import board",
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    FileTypeFilter = { ".json" }
                };
                
                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    // Remove existing board first
                    columns.Clear();
                    ColumnsPanel.Children.Clear();
                    ColumnsPanel.ColumnDefinitions.Clear();
                    
                    boardController.LoadBoardFromJson(file.Path);
                }
            }
            catch (Exception exception)
            {
                var dialog = new ContentDialog()
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = "Failed to import board from file",
                    Content = exception.Message,
                    CloseButtonText = "Ok"
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
                    CommitButtonText = "Export board",
                    SuggestedStartLocation = PickerLocationId.Desktop,
                    FileTypeChoices = {{"JSON Files", new List<string> { ".json" }}}
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
                    Title = "Failed to export board to file",
                    Content = exception.Message,
                    CloseButtonText = "Ok"
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
            columnGrid.ColumnDefinitions.Add( new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            columnGrid.ColumnDefinitions.Add( new ColumnDefinition { Width = GridLength.Auto });

            // Small edit button
            Button editButton = new Button
            {
                Content = new SymbolIcon(Symbol.Edit),
                Padding = new Thickness(6),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(editButton, "Edit column title");
            editButton.Click += (sender, e) => onEditColumnClicked(sender, e, columnId);

            // Small delete button
            Button deleteButton = new Button
            {
                Content = new SymbolIcon(Symbol.Delete),
                Padding = new Thickness(6),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(deleteButton, "Delete column");
            deleteButton.Click += (sender, e) => onDeleteColumnClicked(sender, e, columnId);

            // Regular add button
            Button addTodoToColumnButton = new Button
            {
                Content = new SymbolIcon(Symbol.Add),
                Style = (Style)Application.Current.Resources["PrimaryAction"],
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(addTodoToColumnButton, "Add a new note");
            addTodoToColumnButton.Click += (sender, e) => onAddTodoToColumnClicked(sender, e, columnId);

            // Column name
            TextBlock header = new TextBlock
            {
                Style = (Style)Resources["BoardColumnHeader"],
                Text = columnName,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Stack the edit and delete buttons vertically
            StackPanel columnActions = new StackPanel
            {
                Spacing = 4,
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
                Children = { columnActions, header  }
            };

            ScrollViewer scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,

                Content = new StackPanel
                {
                    Name = columnId.ToString(),
                    Spacing = 8
                }
            };

            Grid.SetColumn(columnActions, 0);
            Grid.SetColumn(header, 1);
            Grid.SetColumn(addTodoToColumnButton, 1);

            Grid.SetRow(scrollViewer, 1);
            Grid.SetColumnSpan(scrollViewer, 2);

            columnGrid.Children.Add(headerLeft);
            columnGrid.Children.Add(addTodoToColumnButton);
            columnGrid.Children.Add(scrollViewer);

            column.Child = columnGrid;

            return column;
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
            var titleBox = new TextBox
            {
                Header = "Title",
                Text = "New ToDo"
            };

            var contentBox = new TextBox
            {
                Header = "Description",
                Text = "This is a new ToDo item.",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100
            };

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Add a new note",
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
            if (result == ContentDialogResult.Primary) { 
                boardController.CreateNote(columnId, titleBox.Text, contentBox.Text); 
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

        private async void onDeleteColumnClicked(object sender, RoutedEventArgs e, Guid columnId)
        {
            string columnName = boardController.GetStatusColumnName(columnId);
            int noteCount = boardController.GetNotesInColumn(columnId).Count;
            List<Guid> otherColumns = boardController.GetStatusColumnIds().FindAll(id => id != columnId);

            TextBlock message = new TextBlock
            {
                Text = $"Delete \"{columnName}\"? This can't be undone.",
                TextWrapping = TextWrapping.Wrap
            };

            // Only ask what to do with the ToDos when there are some, and somewhere to move them
            ComboBox? todoActionBox = null;
            if (noteCount > 0 && otherColumns.Count > 0)
            {
                todoActionBox = new ComboBox
                {
                    Header = $"What should happen to the {noteCount} ToDo(s) in this column?",
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                foreach (Guid otherColumn in otherColumns)
                {
                    todoActionBox.Items.Add(new ComboBoxItem
                    {
                        Content = $"Move to \"{boardController.GetStatusColumnName(otherColumn)}\"",
                        Tag = otherColumn
                    });
                }
                todoActionBox.Items.Add(new ComboBoxItem { Content = "Delete the ToDo(s)" });
                todoActionBox.SelectedIndex = 0;
            }
            else if (noteCount > 0)
            {
                message.Text = $"Delete \"{columnName}\" and the {noteCount} ToDo(s) in it? This can't be undone.";
            }

            StackPanel content = new StackPanel { Spacing = 12 };
            content.Children.Add(message);
            if (todoActionBox != null)
            {
                content.Children.Add(todoActionBox);
            }

            ContentDialog dialog = new ContentDialog
            {
                Title = "Delete column?",
                Content = content,
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
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

        private async void onEditTodoClicked(ToDoNote note)
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

        private async void onAttachFileClicked(ToDoNote note, Button button)
        {
            try
            {
                // Disable the button to avoid double-clicking
                button.IsEnabled = false;
            
                var picker = new FileOpenPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId)
                {
                    CommitButtonText = "Attach file",
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
                    Title = "Failed to attach file",
                    Content = e.Message,
                    CloseButtonText = "Ok"
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
                    Title = "Failed to open attached file",
                    Content = e.Message,
                    CloseButtonText = "Ok"
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
                    Title = "Failed remove attached file",
                    Content = e.Message,
                    CloseButtonText = "Ok"
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
            ToolTipService.SetToolTip(editButton, "Edit");
            editButton.Click += (s, e) => onEditTodoClicked(note);
        
            var attachButton = new Button
            {
                Content = new SymbolIcon(Symbol.Attach),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            ToolTipService.SetToolTip(attachButton, "Attach file");
            attachButton.Click += (s, e) => onAttachFileClicked(note, attachButton);
        
            var deleteButton = new Button
            {
                Content = new SymbolIcon(Symbol.Delete),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            ToolTipService.SetToolTip(deleteButton, "Delete");
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
                ToolTipService.SetToolTip(attachButton, "Remove attached file");
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

            var card = new Border
            {
                Style = (Style)Resources["TodoCard"],
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        titleRow,
                        descriptionRow,
                        attachmentRow
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
