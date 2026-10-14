namespace ToDo_App.Tests;

using ToDo_App;

// ToDoBoard note operations (#56)
public class NoteOperationTests
{
    private ToDoBoard board;
    private Guid plannedId;
    private Guid progressId;
    private ToDoNote first;
    private ToDoNote second;
    private ToDoNote third;
    private ToDoNote other;

    [SetUp]
    public void Setup()
    {
        board = new ToDoBoard();
        plannedId = board.CreateColumn("Planned");
        progressId = board.CreateColumn("In Progress");

        first = new ToDoNote("First note", "Køb bananer");
        second = new ToDoNote("Second note", "Vælg programmeringssprog");
        third = new ToDoNote("Third note", "Australia");
        other = new ToDoNote("Other note", "T-shirt størrelser");
        board.AddNote(plannedId, first);
        board.AddNote(plannedId, second);
        board.AddNote(plannedId, third);
        board.AddNote(progressId, other);
    }

    private List<ToDoNote> planned => board.StatusColumns[0].Notes;
    private List<ToDoNote> progress => board.StatusColumns[1].Notes;

    private void assertBoardUnchanged()
    {
        Assert.That(planned, Is.EqualTo(new[] { first, second, third }));
        Assert.That(progress, Is.EqualTo(new[] { other }));
    }

    [Test]
    public void AddNoteToInvalidColumnThrows()
    {
        var note = new ToDoNote("New", "Content");

        Assert.Throws<KeyNotFoundException>(() => board.AddNote(Guid.NewGuid(), note));
        assertBoardUnchanged();
    }

    [TestCase(-1, 0, 1, 0, TestName = "MoveNoteFromColumnBelowRangeIsNoOp")]
    [TestCase(2, 0, 1, 0, TestName = "MoveNoteFromColumnAboveRangeIsNoOp")]
    [TestCase(0, 0, -1, 0, TestName = "MoveNoteToColumnBelowRangeIsNoOp")]
    [TestCase(0, 0, 2, 0, TestName = "MoveNoteToColumnAboveRangeIsNoOp")]
    [TestCase(0, -1, 1, 0, TestName = "MoveNoteFromNegativePositionIsNoOp")]
    [TestCase(0, 3, 1, 0, TestName = "MoveNoteFromPositionPastEndIsNoOp")]
    [TestCase(0, 0, 1, -1, TestName = "MoveNoteToNegativePositionIsNoOp")]
    [TestCase(0, 0, 1, 2, TestName = "MoveNoteToPositionPastEndOfOtherColumnIsNoOp")]
    [TestCase(0, 0, 0, 3, TestName = "MoveNoteToPositionPastLastNoteInSameColumnIsNoOp")]
    [TestCase(0, 1, 0, 1, TestName = "MoveNoteToSamePositionIsNoOp")]
    public void MoveNoteOutsideBoundsIsNoOp(int fromColumn, int fromPos, int toColumn, int toPos)
    {
        Assert.DoesNotThrow(() => board.MoveNote(fromColumn, fromPos, toColumn, toPos));
        assertBoardUnchanged();
    }

    [Test]
    public void MoveNoteToCountInOtherColumnAppendsToEnd()
    {
        board.MoveNote(0, 0, 1, 1);

        Assert.That(planned, Is.EqualTo(new[] { second, third }));
        Assert.That(progress, Is.EqualTo(new[] { other, first }));
    }

    [Test]
    public void MoveNoteToLastPositionInSameColumnMovesItLast()
    {
        board.MoveNote(0, 0, 0, 2);

        Assert.That(planned, Is.EqualTo(new[] { second, third, first }));
    }

    // Regression target for the MoveNoteDown fix in #51
    [Test]
    public void MoveNoteDownMovesOnePosition()
    {
        board.MoveNoteDown(first.Id);

        Assert.That(planned, Is.EqualTo(new[] { second, first, third }));
    }

    [Test]
    public void MoveNoteUpMovesOnePosition()
    {
        board.MoveNoteUp(third.Id);

        Assert.That(planned, Is.EqualTo(new[] { first, third, second }));
    }

    [Test]
    public void MoveNoteUpOnFirstNoteIsNoOp()
    {
        board.MoveNoteUp(first.Id);

        assertBoardUnchanged();
    }

    [Test]
    public void MoveNoteDownOnLastNoteIsNoOp()
    {
        board.MoveNoteDown(third.Id);

        assertBoardUnchanged();
    }

    [Test]
    public void MoveNoteLeftOnFirstColumnIsNoOpAndKeepsNote()
    {
        board.MoveNoteLeft(second.Id);

        assertBoardUnchanged();
    }

    [Test]
    public void MoveNoteRightOnLastColumnIsNoOpAndKeepsNote()
    {
        board.MoveNoteRight(other.Id);

        assertBoardUnchanged();
    }

    // Decision: moving a note left or right puts it at the top of the new column. This is what the code
    // already does and the UI doesn't use these methods, so it changes nothing the user sees.
    [Test]
    public void MoveNoteRightPutsNoteAtTopOfNextColumn()
    {
        board.MoveNoteRight(third.Id);

        Assert.That(planned, Is.EqualTo(new[] { first, second }));
        Assert.That(progress, Is.EqualTo(new[] { third, other }));
    }

    [Test]
    public void MoveNoteLeftPutsNoteAtTopOfPreviousColumn()
    {
        board.MoveNoteLeft(other.Id);

        Assert.That(planned, Is.EqualTo(new[] { other, first, second, third }));
        Assert.That(progress, Is.Empty);
    }

    [TestCase(nameof(ToDoBoard.MoveNoteUp))]
    [TestCase(nameof(ToDoBoard.MoveNoteDown))]
    [TestCase(nameof(ToDoBoard.MoveNoteLeft))]
    [TestCase(nameof(ToDoBoard.MoveNoteRight))]
    public void MoveNoteWithUnknownIdThrows(string method)
    {
        Action<Guid> move = method switch
        {
            nameof(ToDoBoard.MoveNoteUp) => board.MoveNoteUp,
            nameof(ToDoBoard.MoveNoteDown) => board.MoveNoteDown,
            nameof(ToDoBoard.MoveNoteLeft) => board.MoveNoteLeft,
            _ => board.MoveNoteRight
        };

        Assert.Throws<KeyNotFoundException>(() => move(Guid.NewGuid()));
        assertBoardUnchanged();
    }

    [Test]
    public void MoveNoteToColumnMovesNoteToIndex()
    {
        board.MoveNoteToColumn(second.Id, progressId, 0);

        Assert.That(planned, Is.EqualTo(new[] { first, third }));
        Assert.That(progress, Is.EqualTo(new[] { second, other }));
    }

    [Test]
    public void MoveNoteToColumnWithinSameColumnReorders()
    {
        board.MoveNoteToColumn(first.Id, plannedId, 2);

        Assert.That(planned, Is.EqualTo(new[] { second, third, first }));
    }

    [Test]
    public void MoveNoteToColumnWithUnknownNoteThrows()
    {
        Assert.Throws<KeyNotFoundException>(() => board.MoveNoteToColumn(Guid.NewGuid(), progressId, 0));
        assertBoardUnchanged();
    }

    [Test]
    public void MoveNoteToColumnWithUnknownColumnThrows()
    {
        Assert.Throws<KeyNotFoundException>(() => board.MoveNoteToColumn(first.Id, Guid.NewGuid(), 0));
        assertBoardUnchanged();
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void MoveNoteToColumnOutOfRangeIndexIsNoOp(int index)
    {
        Assert.DoesNotThrow(() => board.MoveNoteToColumn(first.Id, progressId, index));
        assertBoardUnchanged();
    }

    [Test]
    public void SetNoteTitleChangesOnlyTargetNote()
    {
        board.SetNoteTitle(second.Id, "Renamed");

        Assert.That(second.Title, Is.EqualTo("Renamed"));
        Assert.That(new[] { first.Title, third.Title, other.Title },
            Is.EqualTo(new[] { "First note", "Third note", "Other note" }));
    }

    [Test]
    public void SetNoteContentChangesOnlyTargetNote()
    {
        board.SetNoteContent(second.Id, "New content");

        Assert.That(second.Content, Is.EqualTo("New content"));
        Assert.That(new[] { first.Content, third.Content, other.Content },
            Is.EqualTo(new[] { "Køb bananer", "Australia", "T-shirt størrelser" }));
    }

    [Test]
    public void SetNoteFileChangesOnlyTargetNote()
    {
        var file = new AttachedFile("list.txt", Path.Combine("docs", "list.txt"));

        board.SetNoteFile(second.Id, file);

        Assert.That(second.File, Is.SameAs(file));
        Assert.That(new[] { first.File, third.File, other.File }, Is.All.Null);
    }

    [Test]
    public void SetNoteFileWithNullClearsFile()
    {
        board.SetNoteFile(second.Id, new AttachedFile("list.txt", Path.Combine("docs", "list.txt")));

        board.SetNoteFile(second.Id, null);

        Assert.That(second.File, Is.Null);
    }

    [Test]
    public void SetNoteTitleWithUnknownIdThrows()
    {
        Assert.Throws<KeyNotFoundException>(() => board.SetNoteTitle(Guid.NewGuid(), "Renamed"));
    }

    [Test]
    public void SetNoteContentWithUnknownIdThrows()
    {
        Assert.Throws<KeyNotFoundException>(() => board.SetNoteContent(Guid.NewGuid(), "New content"));
    }

    [Test]
    public void SetNoteFileWithUnknownIdThrows()
    {
        Assert.Throws<KeyNotFoundException>(() => board.SetNoteFile(Guid.NewGuid(), null));
    }
}
