namespace ToDo_App.Tests;

using ToDo_App.Controller;
using ToDo_App.Interfaces;

// Regression tests for the bugs found in #61. They started as [Explicit] known-bug tests,
// failed for the stated reasons, and became regular tests once the bugs were fixed.
public class BugRegressionTests
{
    private BoardController controller;
    private EventRecorder recorder;
    private Guid plannedId;
    private Guid progressId;
    private Guid noteId;

    [SetUp]
    public void Setup()
    {
        controller = new LocalBoardController();
        plannedId = controller.CreateColumn("Planned");
        progressId = controller.CreateColumn("In Progress");
        noteId = controller.CreateNote(plannedId, "First note", "Køb bananer");
        recorder = new EventRecorder(controller);
    }

    // Was a NullReferenceException from ToDoBoard.GetColumnIdFromNoteId
    [TestCase(nameof(BoardController.TrashNote))]
    [TestCase(nameof(BoardController.UpdateNote))]
    [TestCase(nameof(BoardController.MoveNoteToColumn))]
    public void UnknownNoteIdThrowsKeyNotFoundExceptionWithoutEvents(string method)
    {
        Guid unknownId = Guid.NewGuid();
        Action operation = method switch
        {
            nameof(BoardController.TrashNote) => () => controller.TrashNote(unknownId),
            nameof(BoardController.UpdateNote) => () => controller.UpdateNote(unknownId, new NoteChanges { Title = "Renamed" }),
            _ => () => controller.MoveNoteToColumn(unknownId, progressId, 0)
        };

        Assert.Throws<KeyNotFoundException>(() => operation());
        Assert.That(recorder.Names, Is.Empty);
        Assert.That(controller.GetNotesInColumn(plannedId).Single().Id, Is.EqualTo(noteId));
    }

    // Loading raised no BoardChanged, so the loaded board wasn't saved and nothing else was told
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

    // The replaced columns got no ColumnRemoved, so a subscribed UI kept showing them
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

    [Test]
    public void LoadBoardFromJsonRaisesRemovedThenCreatedThenBoardChanged()
    {
        string jsonPath = saveOtherBoard();
        try
        {
            controller.LoadBoardFromJson(jsonPath);

            Assert.That(recorder.Names, Is.EqualTo(new[]
            {
                nameof(BoardController.ColumnRemoved),
                nameof(BoardController.ColumnRemoved),
                nameof(BoardController.ColumnCreated),
                nameof(BoardController.BoardChanged)
            }));
        }
        finally
        {
            File.Delete(jsonPath);
        }
    }

    [Test]
    public void LoadBoardFromUnreadableFileKeepsBoardAndRaisesNoEvents()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), $"todo-tests-{Guid.NewGuid()}.json");

        Assert.Throws<FileNotFoundException>(() => controller.LoadBoardFromJson(missingPath));

        Assert.That(recorder.Names, Is.Empty);
        Assert.That(controller.GetStatusColumnIds(), Is.EqualTo(new[] { plannedId, progressId }));
    }

    private static string saveOtherBoard()
    {
        string jsonPath = Path.Combine(Path.GetTempPath(), $"todo-tests-{Guid.NewGuid()}.json");
        var other = new LocalBoardController();
        other.CreateColumn("Loaded column");
        other.SaveBoardToJson(jsonPath);
        return jsonPath;
    }
}
