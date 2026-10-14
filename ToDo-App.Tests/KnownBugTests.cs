namespace ToDo_App.Tests;

using ToDo_App.Controller;
using ToDo_App.Interfaces;

// Suspected bugs (#61). Each test asserts the correct behaviour and fails against the current code.
// They are [Explicit], so a normal test run skips them. Run them with: dotnet test --filter "Category=KnownBug"
// When a bug is fixed, remove [Explicit] so the test becomes a regular regression test.
public class KnownBugTests
{
    private BoardController controller;
    private EventRecorder recorder;
    private Guid plannedId;
    private Guid progressId;
    private Guid noteId;

    [SetUp]
    public void Setup()
    {
        controller = new BoardController();
        plannedId = controller.CreateColumn("Planned");
        progressId = controller.CreateColumn("In Progress");
        noteId = controller.CreateNote(plannedId, "First note", "Køb bananer");
        recorder = new EventRecorder(controller);
    }

    // Currently a NullReferenceException from ToDoBoard.GetColumnIdFromNoteId
    [Explicit("Known bug, see #61")]
    [Category("KnownBug")]
    [TestCase(nameof(BoardController.RemoveNote))]
    [TestCase(nameof(BoardController.MoveNoteUp))]
    [TestCase(nameof(BoardController.MoveNoteDown))]
    [TestCase(nameof(BoardController.MoveNoteLeft))]
    [TestCase(nameof(BoardController.MoveNoteRight))]
    [TestCase(nameof(BoardController.MoveNoteToColumn))]
    public void UnknownNoteIdThrowsKeyNotFoundExceptionWithoutEvents(string method)
    {
        Guid unknownId = Guid.NewGuid();
        Action operation = method switch
        {
            nameof(BoardController.RemoveNote) => () => controller.RemoveNote(unknownId),
            nameof(BoardController.MoveNoteUp) => () => controller.MoveNoteUp(unknownId),
            nameof(BoardController.MoveNoteDown) => () => controller.MoveNoteDown(unknownId),
            nameof(BoardController.MoveNoteLeft) => () => controller.MoveNoteLeft(unknownId),
            nameof(BoardController.MoveNoteRight) => () => controller.MoveNoteRight(unknownId),
            _ => () => controller.MoveNoteToColumn(unknownId, progressId, 0)
        };

        Assert.Throws<KeyNotFoundException>(() => operation());
        Assert.That(recorder.Names, Is.Empty);
        Assert.That(controller.GetNotesInColumn(plannedId).Single().Id, Is.EqualTo(noteId));
    }

    // Currently no BoardChanged is raised after loading
    [Explicit("Known bug, see #61")]
    [Category("KnownBug")]
    [Test]
    public void LoadBoardFromJsonRaisesBoardChanged()
    {
        string jsonPath = saveOtherBoard();
        try
        {
            controller.LoadBoardFromJson(jsonPath);

            Assert.That(recorder.Names, Does.Contain(nameof(BoardController.BoardChanged)));
        }
        finally
        {
            File.Delete(jsonPath);
        }
    }

    // Currently the replaced columns get no ColumnRemoved, so a subscribed UI keeps showing them
    [Explicit("Known bug, see #61")]
    [Category("KnownBug")]
    [Test]
    public void LoadBoardFromJsonRaisesColumnRemovedForReplacedColumns()
    {
        string jsonPath = saveOtherBoard();
        try
        {
            controller.LoadBoardFromJson(jsonPath);

            var removedIds = recorder.ArgsOf<ColumnRemovedEventArgs>().Select(e => e.ColumnId);
            Assert.That(removedIds, Is.EquivalentTo(new[] { plannedId, progressId }));
        }
        finally
        {
            File.Delete(jsonPath);
        }
    }

    // Currently the board changes without any event
    [Explicit("Known bug, see #61")]
    [Category("KnownBug")]
    [Test]
    public void MoveNoteByIndexRaisesContentChangedForBothColumnsThenBoardChanged()
    {
        controller.MoveNote(0, 0, 1, 0);

        Assert.That(recorder.Names, Is.EqualTo(new[]
        {
            nameof(BoardController.ColumnContentChanged),
            nameof(BoardController.ColumnContentChanged),
            nameof(BoardController.BoardChanged)
        }));
        var changedColumns = recorder.ArgsOf<ColumnContentChangedEventArgs>().Select(e => e.ColumnId);
        Assert.That(changedColumns, Is.EqualTo(new[] { plannedId, progressId }));
    }

    private static string saveOtherBoard()
    {
        string jsonPath = Path.Combine(Path.GetTempPath(), $"todo-tests-{Guid.NewGuid()}.json");
        var other = new BoardController();
        other.CreateColumn("Loaded column");
        other.SaveBoardToJson(jsonPath);
        return jsonPath;
    }
}
