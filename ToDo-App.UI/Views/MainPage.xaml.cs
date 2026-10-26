using System.Diagnostics;
using Microsoft.UI.Xaml.Automation;
using System.Globalization;
using Microsoft.Windows.Storage.Pickers;
using ToDo_App.Client;
using ToDo_App.Controller;
using ToDo_App.Interfaces;
using ToDo_App.UI.Appearance;
using ToDo_App.UI.Board;
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

        // The server the Remote toggle connects to. Set TODO_SERVER_URL to use another one
        private const string ServerUrlVariable = "TODO_SERVER_URL";
        private const string DefaultServerUrl = "http://localhost:8080/";
        private Guid selectedTrashNoteId = Guid.Empty;
        private const double ScrollSpeed = 60;
        private const double ScrollTickSpeed = 10;

        // Local by default. The Remote toggle replaces it with a RemoteBoardController (see useBoard)
        ITodoBoard boardController = new LocalBoardController();
        // Every change to the board goes through this, so one on the server's board is grayed out until it's back
        readonly BoardChanges changes;
        bool isSwitchingBoard;

        // The server board while Remote is on, and the test delay for changes from it (developer builds only)
        RemoteBoardController? remoteBoard;
        TimeSpan serverDelay = TimeSpan.Zero;

        List<Guid> columns = new List<Guid>();
        // Which todos are shown. Only affects the view, never the board
        TagFilter tagFilter = new TagFilter();

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

            subscribeTo(boardController);
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
            LanguageSelector.OnLanguageSelected += onLanguageSelected;
            FontSelector.OnFontSelected += onFontChanged;

            this.Loaded += onPageInitialized;

            changes = new BoardChanges(() => boardController, DispatcherQueue);
            changes.Failed += onChangeFailed;

#if DEBUG
            // Only when run as a developer (a Debug build): slow down changes from the server to test how that looks
            ServerDelayBox.Visibility = Visibility.Visible;
#endif
        }

        private void onPageInitialized(object? sender, RoutedEventArgs eventArgs)
        {
            // onFontChanged();
            var language = ((App)Application.Current).Appearance.Current.Language;
            var font = ((App)Application.Current).Appearance.Current.Font;

            if (language != null)
            {
                onLanguageSelected(language);
                if (language != "kln")
                    onFontChanged(font);
            }
            else
            {
                LanguageSelector.SetSelectedLanguage("en-US");
            }
            
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
                        Title = Res.ResourceManager.GetString("error/failed_to_load_board", _currentCulture),
                        Content = defaultFileException.Message,
                        CloseButtonText = Res.ResourceManager.GetString("dialog/ok", _currentCulture)
                    };

                    var result = await dialog.ShowAsync();
                }
            }
        }

        // --- Local or remote board ---

        // Local (the default) keeps the board in a file on this PC, Remote uses the board on the server
        private async void onRemoteToggled(object sender, RoutedEventArgs e)
        {
            // Setting IsOn below raises Toggled again
            if (isSwitchingBoard)
            {
                return;
            }

            isSwitchingBoard = true;
            RemoteToggle.IsEnabled = false;

            if (RemoteToggle.IsOn)
            {
                var remote = new RemoteBoardController(serverUrl()) { IncomingDelay = serverDelay };
                remoteBoard = remote;
                useBoard(remote);
                try
                {
                    // The server's columns arrive through ColumnCreated, like loading a file
                    await remote.ConnectAsync();
                }
                catch (Exception exception)
                {
                    // The server can't be reached: back to the board on this PC
                    RemoteToggle.IsOn = false;
                    useLocalBoard();
                    var dialog = new ContentDialog()
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = String.Format(Res.ResourceManager.GetString("error/failed_to_connect", _currentCulture) ?? "", serverUrl()),
                        Content = exception.Message,
                        CloseButtonText = Res.ResourceManager.GetString("dialog/ok", _currentCulture)
                    };
                    await dialog.ShowAsync();
                }
            }
            else
            {
                useLocalBoard();
            }

            RemoteToggle.IsEnabled = true;
            isSwitchingBoard = false;
        }

        private void useLocalBoard()
        {
            remoteBoard = null;
            useBoard(new LocalBoardController());
            loadInitialBoard();
        }

        // Swaps in another controller. The page is cleared, so the new controller's columns can fill it
        private void useBoard(ITodoBoard board)
        {
            unsubscribeFrom(boardController);
            (boardController as IDisposable)?.Dispose();
            ColumnsPanel.Children.Clear();
            ColumnsPanel.ColumnDefinitions.Clear();
            columns.Clear();
            recalculateColumnsPanelMinWidth();

            boardController = board;
            subscribeTo(boardController);
        }

        private void subscribeTo(ITodoBoard board)
        {
            board.ColumnCreated += onColumnCreated;
            board.ColumnNameChanged += onColumnNameChanged;
            board.ColumnRemoved += onColumnRemoved;
            board.ColumnContentChanged += onColumnContentChanged;
            board.BoardChanged += onBoardChanged;
            board.ColumnOrderChanged += onColumnOrderChanged;
            board.TagChanged += onTagChanged;
        }

        private void unsubscribeFrom(ITodoBoard board)
        {
            board.ColumnCreated -= onColumnCreated;
            board.ColumnNameChanged -= onColumnNameChanged;
            board.ColumnRemoved -= onColumnRemoved;
            board.ColumnContentChanged -= onColumnContentChanged;
            board.BoardChanged -= onBoardChanged;
            board.ColumnOrderChanged -= onColumnOrderChanged;
            board.TagChanged -= onTagChanged;

            // What was on its way to the old board isn't shown as pending on the new one
            Pending.Clear();
            ChangeFailedBar.IsOpen = false;
        }

        // A change the server refused. Nothing changed on the board
        private void onChangeFailed(object? sender, Exception error)
        {
            ChangeFailedBar.Title = Res.ResourceManager.GetString("error/change_not_saved", _currentCulture);
            ChangeFailedBar.Message = error.Message;
            ChangeFailedBar.IsOpen = true;
        }

        private void onServerDelayChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            // An empty box means no delay
            serverDelay = TimeSpan.FromMilliseconds(double.IsNaN(sender.Value) ? 0 : Math.Max(0, sender.Value));
            if (remoteBoard != null)
            {
                remoteBoard.IncomingDelay = serverDelay;
            }
        }

        private static Uri serverUrl()
        {
            return new Uri(Environment.GetEnvironmentVariable(ServerUrlVariable) ?? DefaultServerUrl);
        }

        private void populateStatusColumn(Guid column)
        {
            List<ToDoNote> items = tagFilter.Apply(boardController.GetNotesInColumn(column));
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
            Grid confirmationPanel) 
        {
            listPanel.Children.Clear();

            foreach (ToDoNote note in boardController.GetTrashBinNotes()) {       
                TextBlock title = new TextBlock {
                    Style = (Style)Resources["TodoTitle"],
                    Text = note.Title,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock expandIcon = new TextBlock { Text = ">", VerticalAlignment = VerticalAlignment.Center };

                Grid titleRow = new Grid {
                    ColumnSpacing = 8,
                    ColumnDefinitions = {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }},
                    Children = { expandIcon, title }
                };

                Grid.SetColumn(title, 1);

                TextBlock description = new TextBlock {
                    Style = (Style)Resources["TodoDescription"],
                    Text = note.Content,
                    TextWrapping = TextWrapping.Wrap
                };

                StackPanel expandedContent = new StackPanel {
                    Spacing = 4,
                    Visibility = Visibility.Collapsed,
                    Children = { description }
                };

                if (note.File != null) {
                    var attachedFileButton = new Button {
                        Style = (Style)Application.Current.Resources["AttachedFile"],
                        Content = new StackPanel {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            Children = {
                                new SymbolIcon(Symbol.Attach),
                                new TextBlock { Text = note.File.Name }
                            }
                        }
                    };
                    attachedFileButton.Click += (s, e) => onAttachmentClicked(note);
                    expandedContent.Children.Add(attachedFileButton);
                }

                Button titleButton = new Button {
                    Content = titleRow,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };

                titleButton.Click += (sender, e) => {
                    bool isExpanded = expandedContent.Visibility == Visibility.Visible;

                    expandedContent.Visibility = isExpanded
                        ? Visibility.Collapsed
                        : Visibility.Visible;

                    expandIcon.Text = isExpanded ? ">" : "v";
                };

                StackPanel buttons = new StackPanel {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    VerticalAlignment = VerticalAlignment.Top
                };

                Button restoreButton = new Button{ Content = new SymbolIcon(Symbol.Undo) };
                ToolTipService.SetToolTip(restoreButton, Res.ResourceManager.GetString("dialog/restore_tooltip", _currentCulture));
                restoreButton.Click += (sender, e) => {
                    selectedTrashNoteId = note.Id;
                    restoreHeading.Text = string.Format(
                        Res.ResourceManager.GetString("dialog/restore_which_column", _currentCulture),
                        note.Title);
                    restoreColumnButtons.Children.Clear();
                    List<Guid> columnIds = boardController.GetStatusColumnIds();

                    foreach (Guid columnId in columnIds) {
                        string columnName = boardController.GetStatusColumnName(columnId);

                        TextBlock columnNameText = new TextBlock {
                            Style = (Style)Application.Current.Resources["MyLabel"],
                            Text = $"{columnName}",
                            TextAlignment = TextAlignment.Left,
                            TextWrapping = TextWrapping.Wrap,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };

                        Button destinationButton = new Button {
                            Content = columnNameText,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch
                        };

                        destinationButton.Click += (columnSender, columnE) => {
                            if (selectedTrashNoteId == Guid.Empty) { return; }
                            Guid restoredNoteId = selectedTrashNoteId;
                            changes.Send(board => board.RestoreNote(restoredNoteId, columnId), columnId);
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
                ToolTipService.SetToolTip(deleteButton, Res.ResourceManager.GetString("dialog/permanent_delete_note_tooltip", _currentCulture));
                deleteButton.Click += (sender, e) => { 
                    selectedTrashNoteId = note.Id;
                    confirmationText.Text = string.Format(
                        Res.ResourceManager.GetString("dialog/content_permanent_delete_note", _currentCulture),
                        note.Title);
                    confirmationPanel.Visibility = Visibility.Visible;
                    trashListView.Visibility = Visibility.Collapsed;
                };

                buttons.Children.Add(restoreButton);
                buttons.Children.Add(deleteButton);

                // Put the clickable title and action buttons side by side.
                Grid headerRow = new Grid {
                    ColumnSpacing = 8,
                    ColumnDefinitions = {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto }},
                    Children = { titleButton, buttons }
                };

                Grid.SetColumn(buttons, 1);

                // Assemble the card.
                StackPanel cardContent = new StackPanel { Spacing = 4 };

                cardContent.Children.Add(headerRow);

                if (note.Tags.Count > 0) { cardContent.Children.Add(buildTagsRow(note)); }

                cardContent.Children.Add(expandedContent);

                Border card = new Border {
                    Style = (Style)Resources["TodoCard"],
                    Child = cardContent
                };

                listPanel.Children.Add(card);
            }
        }

        private async void OnTrashBinClicked(object sender, RoutedEventArgs e)
        {
            StackPanel listPanel = new StackPanel { Spacing = 8 };

            Grid trashListView = new Grid {
                RowDefinitions = {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto } },
                RowSpacing = 12,
                MaxHeight = 550
            };

            ScrollViewer trashScrollViewer = new ScrollViewer {
                Content = listPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 500
            };

            //Restore note section
            StackPanel restorePanel = new StackPanel {
                Spacing = 12,
                Visibility = Visibility.Collapsed
            };

            TextBlock restoreHeading = new TextBlock { TextWrapping = TextWrapping.Wrap };

            StackPanel restoreColumnButtons = new StackPanel { Spacing = 8 };

            ScrollViewer restoreColumnScrollViewer = new ScrollViewer {
                Content = restoreColumnButtons,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 400
            };

            Button cancelRestoreButton = new Button {
                Content = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            cancelRestoreButton.Click += (sender, e) => {
                selectedTrashNoteId = Guid.Empty;
                restorePanel.Visibility = Visibility.Collapsed;
                trashListView.Visibility = Visibility.Visible;
            };

            restorePanel.Children.Add(restoreHeading);
            restorePanel.Children.Add(restoreColumnScrollViewer);
            restorePanel.Children.Add(cancelRestoreButton);

            //Confirm delete section
            Grid confirmationPanel = new Grid {
                Visibility = Visibility.Collapsed,
                MinHeight = 180,
                RowDefinitions = {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            TextBlock confirmationText = new TextBlock {
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid confirmationButtons = new Grid {
                ColumnDefinitions = {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                ColumnSpacing = 8,
                Margin = new Thickness(0, 12, 0, 0)
            };

            Button cancelDeleteFromTrashButton = new Button { 
                Content = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            cancelDeleteFromTrashButton.Click += (sender, e) => {
                selectedTrashNoteId = Guid.Empty;
                confirmationPanel.Visibility = Visibility.Collapsed;
                trashListView.Visibility = Visibility.Visible;
            };

            Button confirmDeleteFromTrashButton = new Button {
                Content = Res.ResourceManager.GetString("dialog/delete_permanent", _currentCulture),
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Style = (Style)Application.Current.Resources["PrimaryAction"]
            };
            confirmDeleteFromTrashButton.Click += (sender, e) => {
                if (selectedTrashNoteId == Guid.Empty) { 
                    changes.Send(board => board.EmptyTrash()); 
                }
                else {
                    Guid deletedNoteId = selectedTrashNoteId;
                    changes.Send(board => board.DeleteNote(deletedNoteId));
                    selectedTrashNoteId = Guid.Empty;   
                }
                RefreshTrashBin(listPanel, confirmationText,trashListView, restorePanel, 
                                restoreHeading, restoreColumnButtons, confirmationPanel);
                confirmationPanel.Visibility = Visibility.Collapsed;
                trashListView.Visibility = Visibility.Visible;
            };
            Grid.SetColumn(confirmDeleteFromTrashButton, 0);
            Grid.SetColumn(cancelDeleteFromTrashButton, 1);

            confirmationButtons.Children.Add(confirmDeleteFromTrashButton);
            confirmationButtons.Children.Add(cancelDeleteFromTrashButton);

            Grid.SetRow(confirmationText, 0); Grid.SetRow(confirmationButtons, 1);
            confirmationPanel.Children.Add(confirmationText);
            confirmationPanel.Children.Add(confirmationButtons);

            //Build the trash bin
            Grid trashFooter = new Grid {
                ColumnDefinitions = {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                ColumnSpacing = 8
            };

            Button emptyTrashButton = new Button {
                Content = Res.ResourceManager.GetString("dialog/empty_bin", _currentCulture),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            emptyTrashButton.Click += (sender, e) => {
                selectedTrashNoteId = Guid.Empty;
                confirmationText.Text = Res.ResourceManager.GetString("dialog/empty_bin_confirm", _currentCulture);

                confirmationPanel.Visibility = Visibility.Visible;
                trashListView.Visibility = Visibility.Collapsed;
            };

            Button closeTrashBinButton = new Button {
                Content = Res.ResourceManager.GetString("dialog/close", _currentCulture),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            Grid.SetColumn(emptyTrashButton, 0); Grid.SetColumn(closeTrashBinButton, 1);
            trashFooter.Children.Add(emptyTrashButton);
            trashFooter.Children.Add(closeTrashBinButton);
            
            Grid.SetRow(trashScrollViewer, 0); Grid.SetRow(trashFooter, 1);
            trashListView.Children.Add(trashScrollViewer);
            trashListView.Children.Add(trashFooter);

            Grid dialogContent = new Grid { Width = 500 };
            dialogContent.Children.Add(trashListView);
            dialogContent.Children.Add(restorePanel);
            dialogContent.Children.Add(confirmationPanel);

            RefreshTrashBin(listPanel, confirmationText,trashListView, restorePanel, 
                            restoreHeading, restoreColumnButtons, confirmationPanel);

            ContentDialog dialog = new ContentDialog {
                Title = Res.ResourceManager.GetString("button/trash_bin", _currentCulture),
                Content = dialogContent,
                XamlRoot = this.XamlRoot,
                RequestedTheme = this.ActualTheme
            };
            closeTrashBinButton.Click += (sender, e) => { dialog.Hide(); };

            await dialog.ShowAsync();
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
            Pending.Remove([args.ColumnId, .. args.Notes.Select(note => note.Id)]);

            // A loaded board may not have the filtered tags, and the new column is drawn with the filter
            if (tagFilter.Prune(boardController.GetTags()))
            {
                updateFilterUi();
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
            Pending.Remove([args.ColumnId]);
            
            rebuildColumn(args.ColumnId, args.NewName);
        }

        private void rebuildColumn(Guid id, string name)
        {
            removeColumn(id);
            var column = buildColumn(id, name);
            AddColumnToUI(column, id);
        }

        private void onColumnRemoved(object? sender, EventArgs eventArgs)
        {
            if (eventArgs is not ColumnRemovedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }
            Pending.Remove([args.ColumnId]);

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
            Pending.Remove([args.ColumnId, .. args.NewNotes.Select(note => note.Id)]);
            
            // The filter turns off tags that are gone, which can show todos in every column
            if (tagFilter.Prune(boardController.GetTags()))
            {
                refreshFilter();
                return;
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
            Pending.Remove([args.TagId]);

            // Redraw every column with a card that shows the tag
            foreach (Guid columnId in columns)
            {
                if (boardController.GetNotesInColumn(columnId).Exists(note => note.Tags.Exists(tag => tag.Id == args.TagId)))
                {
                    clearColumnItems(columnId);
                    populateStatusColumn(columnId);
                }
            }

            // A renamed or recoloured tag is also shown in the filter bar
            updateFilterUi();
        }

        private void onColumnOrderChanged(object? sender, EventArgs e)
        {
            if (e is not ColumnOrderChangedEventArgs args)
            {
                throw new ArgumentException("Wrong EventArgs type");
            }
            Pending.Remove(args.ColumnIds);

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

            // The server saves a remote board. board.json is only for the board on this PC
            if (boardController is RemoteBoardController)
            {
                return;
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
            Pending.SetIds(column, columnId);

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
                // The cards are only the shown todos, so hidden todos keep their positions
                dropIndex = tagFilter.ToColumnIndex(notes, dropIndex);
                int currentIndex = notes.FindIndex(n => n.Id == noteId);
                if (currentIndex != -1 && currentIndex < dropIndex)
                {
                    dropIndex--;
                }
                changes.Send(board => board.MoveNoteToColumn(noteId, columnId, dropIndex), noteId);
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

                changes.Send(board => board.MoveColumn(draggedColumnId, dropIndex), draggedColumnId);
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

            var contentScrollViewer = new ScrollViewer
            {
                Content = contentBox,
                MaxHeight = 400,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var tagEditor = new TagEditor(boardController, changes, Array.Empty<NoteTag>(), tagEditorStrings());

            var dialog = new ThemedContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = Res.ResourceManager.GetString("dialog/title_add_new_note", _currentCulture),
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/add", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = this.ActualTheme,
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children = { titleBox, contentScrollViewer, tagEditor }
                }
            };
            // Keep the dialog open if a typed tag isn't valid, so the user can see why
            dialog.PrimaryButtonClick += (s, args) => args.Cancel = !tagEditor.CommitPendingInput();

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary) {
                // Leaving a field empty keeps the placeholder text, as it did when the fields were prefilled
                string title = string.IsNullOrWhiteSpace(titleBox.Text) ? defaultTitle : titleBox.Text;
                string content = string.IsNullOrWhiteSpace(contentBox.Text) ? defaultContent : contentBox.Text;
                var tagNames = tagEditor.TagNames;
                var newTagColors = tagEditor.NewTagColors;
                changes.Send(board => board.CreateNote(columnId, title, content, tagNames: tagNames, newTagColors: newTagColors), columnId);
            }
        }

        private async void onEditColumnClicked(object sender, RoutedEventArgs e, Guid columnId)
        {
            TextBox nameBox = new TextBox
            {
                Text = boardController.GetStatusColumnName(columnId)
            };

            ContentDialog dialog = new ThemedContentDialog
            {
                Title = Res.ResourceManager.GetString("dialog/title_edit_column", _currentCulture),
                Content = nameBox,
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/save", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = this.ActualTheme,
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string newName = nameBox.Text;
                changes.Send(board => board.UpdateColumn(columnId, newName), columnId);
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
                var headerText = Localizer.HandlePlurals(
                    Res.ResourceManager.GetString("dialog/content_delete_column_consequence", _currentCulture),
                    noteCount, _currentCulture.Name);
                var deleteText = Localizer.HandlePlurals(
                    Res.ResourceManager.GetString("dialog/delete_the_todos", _currentCulture),
                    noteCount, _currentCulture.Name);
                todoActionBox = new ComboBox
                {
                    Header = headerText,
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
                todoActionBox.Items.Add(new ComboBoxItem { Content = deleteText });
                todoActionBox.SelectedIndex = 0;
            }
            else if (noteCount > 0)
            {
                var textWithNumber = Localizer.HandlePlurals(
                    Res.ResourceManager.GetString("dialog/content_delete_column_no_space", _currentCulture),
                    noteCount, _currentCulture.Name, 1);
                message.Text = String.Format(textWithNumber, columnName);
            }

            StackPanel content = new StackPanel { Spacing = 12 };
            content.Children.Add(message);
            if (todoActionBox != null)
            {
                content.Children.Add(todoActionBox);
            }

            ContentDialog dialog = new ThemedContentDialog
            {
                Title = Res.ResourceManager.GetString("dialog/title_delete_column_q", _currentCulture),
                Content = content,
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/delete_permanent", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = this.ActualTheme,
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (todoActionBox?.SelectedItem is ComboBoxItem { Tag: Guid moveToColumn })
                {
                    changes.Send(board => board.DeleteColumn(columnId, moveToColumn), columnId, moveToColumn);
                }
                else
                {
                    changes.Send(board => board.DeleteColumn(columnId), columnId);
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
            var dialog = new ThemedContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = Res.ResourceManager.GetString("dialog/title_remove_note_q", _currentCulture),
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/remove", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                Content = String.Format(Res.ResourceManager.GetString("dialog/content_remove_note", _currentCulture), note.Title),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = this.ActualTheme
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                changes.Send(board => board.TrashNote(note.Id), note.Id);
            }
        }

        private async void onEditTodoClicked(ToDoNote note)
        {
            // The note as the dialog opens, so saving only changes what the user changed. The tag names are copied,
            // as renaming a tag changes the note's tags in place
            string openedTitle = note.Title;
            string openedContent = note.Content;
            List<string> openedTagNames = note.Tags.Select(tag => tag.Name).ToList();
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

            var contentScrollViewer = new ScrollViewer
            {
                Content = contentBox,
                MaxHeight = 400,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            
            var tagEditor = new TagEditor(boardController, changes, note.Tags, tagEditorStrings());

            var dialog = new ThemedContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = Res.ResourceManager.GetString("dialog/title_edit_note", _currentCulture),
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/save", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = this.ActualTheme,
                Content = new StackPanel
                {
                    Spacing = 8,
                    Children = { titleBox, contentScrollViewer, tagEditor }
                }
            };
            // Keep the dialog open if a typed tag isn't valid, so the user can see why
            dialog.PrimaryButtonClick += (s, args) => args.Cancel = !tagEditor.CommitPendingInput();

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Only what the user changed, so a change someone else made meanwhile isn't undone.
                // Tags first: they're the only part that can be refused
                var tagNames = tagEditor.TagNames;
                var newTagColors = tagEditor.NewTagColors;
                string title = titleBox.Text;
                string content = contentBox.Text;
                bool tagsChanged = !tagNames.SequenceEqual(openedTagNames);
                bool titleChanged = title != openedTitle;
                bool contentChanged = content != openedContent;
                if (tagsChanged || titleChanged || contentChanged)
                {
                    changes.Send(board =>
                    {
                        if (tagsChanged)
                        {
                            board.UpdateNoteTags(note.Id, tagNames, newTagColors);
                        }
                        if (titleChanged)
                        {
                            board.UpdateNoteTitle(note.Id, title);
                        }
                        if (contentChanged)
                        {
                            board.UpdateNoteContent(note.Id, content);
                        }
                    }, note.Id);
                }
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
                    var attachment = new AttachedFile(System.IO.Path.GetFileName(file.Path), file.Path);
                    changes.Send(board => board.UpdateNoteFile(note.Id, attachment), note.Id);
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
                changes.Send(board => board.DeleteNoteFile(note.Id), note.Id);
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

            var noteTextPanel = new StackPanel
            {
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Top,
                Children = { titleText }
            };
            if (note.Tags.Count > 0) { noteTextPanel.Children.Add(buildTagsRow(note)); }
            noteTextPanel.Children.Add(descriptionText);
            
            var editButton = new Button { Content = new SymbolIcon(Symbol.Edit) };
            ToolTipService.SetToolTip(editButton, Res.ResourceManager.GetString("button/edit_note_tooltip", _currentCulture));
            editButton.Click += (s, e) => onEditTodoClicked(note);
        
            var attachButton = new Button { Content = new SymbolIcon(Symbol.Attach) };
            ToolTipService.SetToolTip(attachButton, Res.ResourceManager.GetString("button/attach_file_tooltip", _currentCulture));
            attachButton.Click += (s, e) => onAttachFileClicked(note, attachButton);
        
            var deleteButton = new Button { Content = new SymbolIcon(Symbol.Delete) };
            ToolTipService.SetToolTip(deleteButton, Res.ResourceManager.GetString("button/remove_note_tooltip", _currentCulture));
            deleteButton.Click += (s, e) => onDeleteTodoClicked(note);

            var noteActionButtons = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Top,
                Children = { editButton, attachButton, deleteButton }
            };

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
                ToolTipService.SetToolTip(removeAttachmentButton, Res.ResourceManager.GetString("button/remove_attached_file_tooltip", _currentCulture));
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
                noteTextPanel.Children.Add(attachmentButtonsStack);
            }          

            var cardContent = new Grid
            {
                ColumnSpacing = 8,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children = { noteTextPanel, noteActionButtons }
            };
            Grid.SetColumn(noteActionButtons, 1);

            var card = new Border
            {
                Style = (Style)Resources["TodoCard"],
                CanDrag = true,
                Child = cardContent
            };
            card.DragStarting += (s, e) => onNote_DragStarting(note, e);
            card.DropCompleted += (s, e) => stopAutoScroll();
            // Grayed out while a change to the todo, or to one of its tags, is on its way
            Pending.SetIds(card, [note.Id, .. note.Tags.Select(tag => tag.Id)]);
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

        // --- Filter the board by tags (#67, #69) ---

        private string filterString(string key)
        {
            return Res.ResourceManager.GetString(key, _currentCulture) ?? key;
        }

        // Rebuilt every time it opens, so it lists the tags the board has right now
        private void onFilterFlyoutOpening(object? sender, object e)
        {
            FilterFlyoutContent.Children.Clear();
            List<NoteTag> tags = boardController.GetTags();
            if (tags.Count == 0)
            {
                FilterFlyoutContent.Children.Add(new TextBlock
                {
                    Text = filterString("filter/no_tags"),
                    TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            foreach (NoteTag tag in tags)
            {
                Guid tagId = tag.Id;
                var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                content.Children.Add(new Microsoft.UI.Xaml.Shapes.Ellipse
                {
                    Width = 10,
                    Height = 10,
                    VerticalAlignment = VerticalAlignment.Center,
                    Fill = new SolidColorBrush(AccentPalette.Find(tag.Color).Color)
                });
                content.Children.Add(new TextBlock { Text = tag.Name, VerticalAlignment = VerticalAlignment.Center });

                var checkBox = new CheckBox { Content = content, IsChecked = tagFilter.TagIds.Contains(tagId) };
                AutomationProperties.SetName(checkBox, tag.Name);
                checkBox.Checked += (s, args) => onFilterTagToggled(tagId, true);
                checkBox.Unchecked += (s, args) => onFilterTagToggled(tagId, false);
                FilterFlyoutContent.Children.Add(checkBox);
            }

            // How several selected tags are combined
            FilterFlyoutContent.Children.Add(new TextBlock { Text = filterString("filter/match_label") });
            var matchAny = new RadioButton
            {
                Content = filterString("filter/match_any"),
                GroupName = "TagMatchMode",
                IsChecked = tagFilter.Mode == TagMatchMode.Any
            };
            var matchAll = new RadioButton
            {
                Content = filterString("filter/match_all"),
                GroupName = "TagMatchMode",
                IsChecked = tagFilter.Mode == TagMatchMode.All
            };
            matchAny.Checked += (s, args) => onFilterModeChanged(TagMatchMode.Any);
            matchAll.Checked += (s, args) => onFilterModeChanged(TagMatchMode.All);
            FilterFlyoutContent.Children.Add(matchAny);
            FilterFlyoutContent.Children.Add(matchAll);

            var clearButton = new Button { Content = filterString("filter/clear") };
            clearButton.Click += (s, args) =>
            {
                FilterFlyout.Hide();
                onClearFilterClicked(s, args);
            };
            FilterFlyoutContent.Children.Add(clearButton);
        }

        private void onFilterTagToggled(Guid tagId, bool isSelected)
        {
            if (isSelected)
            {
                tagFilter.Select(tagId);
            }
            else
            {
                tagFilter.Deselect(tagId);
            }
            refreshFilter();
        }

        private void onFilterModeChanged(TagMatchMode mode)
        {
            tagFilter.Mode = mode;
            refreshFilter();
        }

        private void onClearFilterClicked(object sender, RoutedEventArgs e)
        {
            tagFilter.Clear();
            refreshFilter();
        }

        // Redraws every column with the current filter. The board itself is untouched
        private void refreshFilter()
        {
            foreach (Guid columnId in columns)
            {
                clearColumnItems(columnId);
                populateStatusColumn(columnId);
            }
            updateFilterUi();
        }

        // Shows on the toolbar that the board is filtered, and by which tags
        private void updateFilterUi()
        {
            var activeText = Localizer.HandlePlurals(filterString("filter/button_active"),
                tagFilter.TagIds.Count, _currentCulture.Name);
            FilterButton.Content = tagFilter.IsActive
                ? activeText
                : filterString("filter/button");
            FilterBar.Visibility = tagFilter.IsActive ? Visibility.Visible : Visibility.Collapsed;
            FilterBarLabel.Text = filterString("filter/showing");
            ClearFilterButton.Content = filterString("filter/clear");
            FilterBarMode.Text = tagFilter.TagIds.Count > 1
                ? filterString(tagFilter.Mode == TagMatchMode.All ? "filter/match_all" : "filter/match_any")
                : "";

            FilterBarTags.Children.Clear();
            List<NoteTag> boardTags = boardController.GetTags();
            foreach (Guid tagId in tagFilter.TagIds)
            {
                NoteTag? tag = boardTags.Find(candidate => candidate.Id == tagId);
                if (tag != null)
                {
                    FilterBarTags.Children.Add(buildFilterTag(tag));
                }
            }
        }

        // A tag in the filter bar: its name in its colour, and a ✕ to stop filtering by it
        private Border buildFilterTag(NoteTag tag)
        {
            Guid tagId = tag.Id;
            var removeButton = new Button
            {
                Content = new FontIcon { Glyph = "", FontSize = 10, Foreground = new SolidColorBrush(Colors.White) },
                Padding = new Thickness(4),
                MinWidth = 0,
                MinHeight = 0,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0)
            };
            string label = string.Format(filterString("filter/remove_tag"), tag.Name);
            AutomationProperties.SetName(removeButton, label);
            ToolTipService.SetToolTip(removeButton, label);
            removeButton.Click += (s, args) => onFilterTagToggled(tagId, false);

            var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
            content.Children.Add(new TextBlock
            {
                Style = (Style)Resources["TodoTagText"],
                Text = tag.Name,
                VerticalAlignment = VerticalAlignment.Center
            });
            content.Children.Add(removeButton);

            return new Border
            {
                Style = (Style)Resources["TodoTag"],
                Padding = new Thickness(8, 0, 2, 0),
                Background = new SolidColorBrush(AccentPalette.Find(tag.Color).Color),
                Child = content
            };
        }

        private Dictionary<string, string> tagEditorStrings()
        {
            string[] keys =
            {
                "todo/property_tags", "dialog/add_tag_placeholder", "dialog/remove_tag", "dialog/edit_tag",
                "dialog/new_tag_tooltip", "dialog/tag_name", "dialog/tag_used_on", "dialog/tag_new_info",
                "dialog/tag_name_taken", "dialog/tag_colour", "dialog/rename", "dialog/delete", "dialog/cancel", "dialog/delete_tag_confirm", "dialog/merge", "dialog/merge_tag_confirm"
            };
            // A missing translation shows the key instead of crashing the dialog
            var dict = keys.ToDictionary(key => key, key => Res.ResourceManager.GetString(key, _currentCulture) ?? key);
            dict.Add("lang_code", _currentCulture.Name);
            return dict;
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

            ContentDialog dialog = new ThemedContentDialog
            {
                Title = Res.ResourceManager.GetString("dialog/title_add_new_column", _currentCulture),
                Content = columnNameBox,
                PrimaryButtonText = Res.ResourceManager.GetString("dialog/add", _currentCulture),
                CloseButtonText = Res.ResourceManager.GetString("dialog/cancel", _currentCulture),
                DefaultButton = ContentDialogButton.Primary,
                RequestedTheme = this.ActualTheme,
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string columnName = columnNameBox.Text;

                changes.Send(board => board.CreateColumn(columnName));
            }
        }

        void onLanguageSelected(string language)
        {
            var refreshThroughFontChange = language == "kln" || _currentCulture != null && _currentCulture.Name == "kln";
            
            _currentCulture = new CultureInfo(language);

            if (refreshThroughFontChange)
                onFontChanged(((App)Application.Current).Appearance.Current.Font);
            else
                refreshUi();
        }

        void onFontChanged(string? selectedFont)
        {
            var language = _currentCulture.Name;
            var defaultFont = language == "kln" ? "Georgia" : "Segoe UI";
            var newFont = selectedFont ?? defaultFont;
            
            var resources = Application.Current.Resources;
            resources["AppFontFamily"] = newFont;
            resources["ContentControlThemeFontFamily"] = newFont;
            resources["MTCMediaFontFamily"] = newFont;
            resources["PhoneFontFamilyNormal"] = newFont;
            resources["PhoneFontFamilySemiLight"] = newFont;
            resources["PivotHeaderItemFontFamily"] = newFont;
            resources["PivotTitleFontFamily"] = newFont;
            resources["KeyTipFontFamily"] = newFont;
            
            var font = (string)Application.Current.Resources["AppFontFamily"];
            AddColumnButtonTextBlock.FontFamily = new FontFamily(font);
            ImportButtonTextBlock.FontFamily = new FontFamily(font);
            ExportButtonTextBlock.FontFamily = new FontFamily(font);
            TrashBinButtonTextBlock.FontFamily = new FontFamily(font);
            FilterButton.FontFamily = new FontFamily(font);
            ((ComboBox)LanguageSelector.FindName("LanguageComboBox")).FontFamily = new FontFamily(font);

            refreshUi();
        }
        
        private void refreshUi()
        {
            // Refresh static UI elements
            AddColumnButtonTextBlock.Text = Res.ResourceManager.GetString("button/add_column", _currentCulture);
            ImportButtonTextBlock.Text = Res.ResourceManager.GetString("button/import", _currentCulture);
            ExportButtonTextBlock.Text = Res.ResourceManager.GetString("button/export", _currentCulture);
            RemoteToggle.OffContent = Res.ResourceManager.GetString("button/board_local", _currentCulture);
            RemoteToggle.OnContent = Res.ResourceManager.GetString("button/board_remote", _currentCulture);
            ToolTipService.SetToolTip(RemoteToggle, String.Format(
                Res.ResourceManager.GetString("button/board_source_tooltip", _currentCulture) ?? "", serverUrl()));
            TrashBinButtonTextBlock.Text = Res.ResourceManager.GetString("button/trash_bin", _currentCulture);
            
            // Refresh toolbar components
            updateFilterUi();
            AppearanceSelector.SetStrings(
                Res.ResourceManager.GetString("button/tooltip_light", _currentCulture),
                Res.ResourceManager.GetString("button/tooltip_system_default", _currentCulture),
                Res.ResourceManager.GetString("button/tooltip_dark", _currentCulture),
                Res.ResourceManager.GetString("button/tooltip_accent_color", _currentCulture)
                );
            FontSelector.SetLocalizedStrings(Res.ResourceManager.GetString("button/font_tooltip", _currentCulture));
            LanguageSelector.SetLocalizedStrings(Res.ResourceManager.GetString("button/language_tooltip", _currentCulture));
            
            // Refresh dynamic UI elements for each column
            foreach (var guid in columns)
            {
                rebuildColumn(guid, boardController.GetStatusColumnName(guid));
            }
        }
    }
}
