using ToDo_App.Controller;
using ToDo_App.Interfaces;

namespace ToDo_App.Web.Board;

/// <summary>
/// Owns the one board the server shows, and keeps it saved. Every browser tab uses the same
/// <see cref="Board"/>, so a change in one tab reaches the others through the <see cref="ITodoBoard"/> events.
/// </summary>
/// <remarks>
/// This is what MainPage does in the Windows app: load <c>board.json</c>, or the default board if that fails,
/// and save on every <see cref="ITodoBoard.BoardChanged"/>. It lives here instead of in the page, because a
/// page exists once per tab and the board must only be loaded and saved once.
/// </remarks>
public sealed class BoardHost
{
    private const string DefaultJsonPath = "Assets/default_board.json";
    private const string UserJsonFile = "board.json";

    private readonly string userJsonPath;
    private readonly ILogger<BoardHost> logger;

    public BoardHost(BoardStorageOptions options, ILogger<BoardHost> logger)
    {
        this.logger = logger;
        Directory.CreateDirectory(options.DataDirectory);
        userJsonPath = Path.Combine(options.DataDirectory, UserJsonFile);

        loadInitialBoard();
        // Subscribed after loading, so falling back to the default board doesn't overwrite a board.json
        // that couldn't be read. It's saved once the user changes something
        Board.BoardChanged += onBoardChanged;
    }

    public ITodoBoard Board { get; } = new LocalBoardController();

    /// <summary>Set when loading failed, so a page can tell the user. Null when the board loaded.</summary>
    public string? LoadError { get; private set; }

    /// <summary>Set when the last save failed, and cleared by the next successful save.</summary>
    public string? SaveError { get; private set; }

    /// <summary>Raised after every save attempt, so pages can show or hide <see cref="SaveError"/>.</summary>
    public event EventHandler? SaveCompleted;

    private void loadInitialBoard()
    {
        try
        {
            Board.LoadBoardFromJson(userJsonPath);
        }
        catch (Exception userFileException)
        {
            logger.LogInformation("Could not load {Path}, using the default board: {Message}", userJsonPath, userFileException.Message);
            // If the user board can't be loaded, load the default board instead
            try
            {
                Board.LoadBoardFromJson(Path.Combine(AppContext.BaseDirectory, DefaultJsonPath));
            }
            catch (Exception defaultFileException)
            {
                logger.LogError(defaultFileException, "Could not load the default board");
                LoadError = defaultFileException.Message;
            }
        }
    }

    private void onBoardChanged(object? sender, EventArgs eventArgs)
    {
        try
        {
            Board.SaveBoardToJson(userJsonPath);
            SaveError = null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Could not save {Path}", userJsonPath);
            SaveError = e.Message;
        }
        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Finds a todo by its Id, or null if no column has it.</summary>
    public ToDoNote? FindNote(Guid noteId)
    {
        return Board.GetStatusColumnIds()
            .SelectMany(Board.GetNotesInColumn)
            .FirstOrDefault(note => note.Id == noteId);
    }
}
