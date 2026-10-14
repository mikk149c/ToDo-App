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
        controller = new BoardController();
        plannedId = controller.CreateColumn("Planned");
        progressId = controller.CreateColumn("In Progress");
        noteId = controller.CreateNote(plannedId, "First note", "Køb bananer");
        recorder = new EventRecorder(controller);
    }

    // Was a NullReferenceException from ToDoBoard.GetColumnIdFromNoteId
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

    // The board changed without any event
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

    [TestCase(-1, 0, TestName = "MoveNoteByIndexFromInvalidColumnRaisesNoEvents")]
    [TestCase(0, 1, TestName = "MoveNoteByIndexFromEmptyPositionRaisesNoEvents")]
    public void MoveNoteByIndexWithNothingToMoveRaisesNoEvents(int fromColumnIndex, int fromPos)
    {
        controller.MoveNote(fromColumnIndex, fromPos, 1, 0);

        Assert.That(recorder.Names, Is.Empty);
        Assert.That(controller.GetNotesInColumn(plannedId).Single().Id, Is.EqualTo(noteId));
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
        var other = new BoardController();
        other.CreateColumn("Loaded column");
        other.SaveBoardToJson(jsonPath);
        return jsonPath;
    }
}
