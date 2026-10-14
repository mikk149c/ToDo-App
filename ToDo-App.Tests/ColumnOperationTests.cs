namespace ToDo_App.Tests;

using ToDo_App;

// ToDoBoard column operations (#55)
public class ColumnOperationTests
{
    private ToDoBoard board;
    private Guid plannedId;
    private Guid progressId;
    private Guid doneId;

    [SetUp]
    public void Setup()
    {
        board = new ToDoBoard();
        plannedId = board.CreateColumn("Planned");
        progressId = board.CreateColumn("In Progress");
        doneId = board.CreateColumn("Done");
    }

    private List<Guid> columnOrder() => board.StatusColumns.Select(column => column.Id).ToList();

    [Test]
    public void CreateColumnReturnsUniqueIdAndAppendsToEnd()
    {
        Guid newId = board.CreateColumn("Review");

        Assert.That(columnOrder(), Is.EqualTo(new[] { plannedId, progressId, doneId, newId }));
        Assert.That(columnOrder(), Is.Unique);
        Assert.That(board.StatusColumns[3].Title, Is.EqualTo("Review"));
        Assert.That(board.StatusColumns[3].Notes, Is.Empty);
    }

    // Decision: titles aren't validated. The UI lets you create a column with an empty name,
    // and adding validation would be a new feature, so empty and null are stored as given.
    [TestCase("")]
    [TestCase(null)]
    public void CreateColumnAcceptsEmptyOrNullTitleAsGiven(string? title)
    {
        Guid newId = board.CreateColumn(title!);

        Assert.That(board.StatusColumns[3].Id, Is.EqualTo(newId));
        Assert.That(board.StatusColumns[3].Title, Is.EqualTo(title));
    }

    [Test]
    public void MoveColumnForwardKeepsOtherColumnsInOrder()
    {
        board.MoveColumn(0, 2);

        Assert.That(columnOrder(), Is.EqualTo(new[] { progressId, doneId, plannedId }));
    }

    [Test]
    public void MoveColumnBackwardKeepsOtherColumnsInOrder()
    {
        board.MoveColumn(2, 0);

        Assert.That(columnOrder(), Is.EqualTo(new[] { doneId, plannedId, progressId }));
    }

    [TestCase(-1, 0)]
    [TestCase(3, 0)]
    [TestCase(0, -1)]
    [TestCase(0, 3)]
    [TestCase(1, 1)]
    public void MoveColumnOutOfRangeOrSameIndexIsNoOp(int fromIndex, int toIndex)
    {
        Assert.DoesNotThrow(() => board.MoveColumn(fromIndex, toIndex));

        Assert.That(columnOrder(), Is.EqualTo(new[] { plannedId, progressId, doneId }));
    }

    [Test]
    public void MoveColumnByIdMovesThatColumn()
    {
        board.MoveColumn(doneId, 1);

        Assert.That(columnOrder(), Is.EqualTo(new[] { plannedId, doneId, progressId }));
    }

    [Test]
    public void MoveColumnByInvalidIdThrowsAndLeavesOrderUnchanged()
    {
        Assert.Throws<KeyNotFoundException>(() => board.MoveColumn(Guid.NewGuid(), 0));

        Assert.That(columnOrder(), Is.EqualTo(new[] { plannedId, progressId, doneId }));
    }

    [Test]
    public void MoveAllNotesAppendsAfterTargetNotesInOrderAndEmptiesSource()
    {
        var existingNote = new ToDoNote("Existing", "Australia");
        var firstMoved = new ToDoNote("First", "Køb bananer");
        var secondMoved = new ToDoNote("Second", "Vælg programmeringssprog");
        board.AddNote(progressId, existingNote);
        board.AddNote(plannedId, firstMoved);
        board.AddNote(plannedId, secondMoved);

        board.MoveAllNotes(plannedId, progressId);

        Assert.That(board.StatusColumns[0].Notes, Is.Empty);
        Assert.That(board.StatusColumns[1].Notes, Is.EqualTo(new[] { existingNote, firstMoved, secondMoved }));
    }

    [Test]
    public void MoveAllNotesToSameColumnIsNoOp()
    {
        var firstNote = new ToDoNote("First", "Køb bananer");
        var secondNote = new ToDoNote("Second", "Vælg programmeringssprog");
        board.AddNote(plannedId, firstNote);
        board.AddNote(plannedId, secondNote);

        board.MoveAllNotes(plannedId, plannedId);

        // No notes lost or duplicated
        Assert.That(board.StatusColumns[0].Notes, Is.EqualTo(new[] { firstNote, secondNote }));
    }

    [Test]
    public void MoveAllNotesFromInvalidColumnThrows()
    {
        var note = new ToDoNote("First", "Køb bananer");
        board.AddNote(progressId, note);

        Assert.Throws<KeyNotFoundException>(() => board.MoveAllNotes(Guid.NewGuid(), progressId));

        Assert.That(board.StatusColumns[1].Notes, Is.EqualTo(new[] { note }));
    }

    [Test]
    public void MoveAllNotesToInvalidColumnThrowsAndKeepsSourceNotes()
    {
        var note = new ToDoNote("First", "Køb bananer");
        board.AddNote(plannedId, note);

        Assert.Throws<KeyNotFoundException>(() => board.MoveAllNotes(plannedId, Guid.NewGuid()));

        Assert.That(board.StatusColumns[0].Notes, Is.EqualTo(new[] { note }));
    }

    // Removing a column is an explicit delete, so its notes go with it
    [Test]
    public void RemoveColumnWithNotesDiscardsThoseNotes()
    {
        var removedNote = new ToDoNote("Removed", "Køb bananer");
        var keptNote = new ToDoNote("Kept", "Australia");
        board.AddNote(plannedId, removedNote);
        board.AddNote(progressId, keptNote);

        board.RemoveColumn(plannedId);

        Assert.That(columnOrder(), Is.EqualTo(new[] { progressId, doneId }));
        var remainingNotes = board.StatusColumns.SelectMany(column => column.Notes).ToList();
        Assert.That(remainingNotes, Is.EqualTo(new[] { keptNote }));
    }
}
