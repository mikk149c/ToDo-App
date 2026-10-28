namespace ToDo_App.Tests;

using ToDo_App.Controller;
using ToDo_App.Interfaces;

// BoardController events (#58). State-changing operations raise their matching event, then BoardChanged.
public class ControllerEventTests
{
    private BoardController controller;
    private EventRecorder recorder;
    private Guid plannedId;
    private Guid progressId;
    private Guid firstNoteId;
    private Guid secondNoteId;

    [SetUp]
    public void Setup()
    {
        controller = new LocalBoardController();
        plannedId = controller.CreateColumn("Planned");
        progressId = controller.CreateColumn("In Progress");
        firstNoteId = controller.CreateNote(plannedId, "First note", "Køb bananer");
        secondNoteId = controller.CreateNote(plannedId, "Second note", "Vælg programmeringssprog");

        // Only record what the operation under test raises
        recorder = new EventRecorder(controller);
    }

    private void assertContentChangedFor(params Guid[] columnIds)
    {
        var expectedNames = columnIds.Select(_ => nameof(BoardController.ColumnContentChanged))
            .Append(nameof(BoardController.BoardChanged));
        Assert.That(recorder.Names, Is.EqualTo(expectedNames));

        var contentChanges = recorder.ArgsOf<ColumnContentChangedEventArgs>();
        Assert.That(contentChanges.Select(e => e.ColumnId), Is.EqualTo(columnIds));
        foreach (var change in contentChanges)
        {
            Assert.That(change.NewNotes, Is.EqualTo(controller.GetNotesInColumn(change.ColumnId)));
        }
    }

    [Test]
    public void CreateColumnRaisesColumnCreatedThenBoardChanged()
    {
        Guid newId = controller.CreateColumn("Done");

        Assert.That(recorder.Names, Is.EqualTo(new[] { nameof(BoardController.ColumnCreated), nameof(BoardController.BoardChanged) }));
        var created = recorder.ArgsOf<ColumnCreatedEventArgs>().Single();
        Assert.That(created.ColumnId, Is.EqualTo(newId));
        Assert.That(created.Name, Is.EqualTo("Done"));
        Assert.That(created.Notes, Is.Empty);
    }

    [Test]
    public void UpdateColumnRaisesColumnNameChangedThenBoardChanged()
    {
        controller.UpdateColumn(plannedId, "Backlog");

        Assert.That(recorder.Names, Is.EqualTo(new[] { nameof(BoardController.ColumnNameChanged), nameof(BoardController.BoardChanged) }));
        var renamed = recorder.ArgsOf<ColumnNameChangedEventArgs>().Single();
        Assert.That(renamed.ColumnId, Is.EqualTo(plannedId));
        Assert.That(renamed.NewName, Is.EqualTo("Backlog"));
    }

    [Test]
    public void BoardChangedCarriesTheBoard()
    {
        controller.UpdateColumn(plannedId, "Backlog");

        var board = recorder.ArgsOf<BoardChangedEventArgs>().Single().Board;
        Assert.That(board.StatusColumns.Select(column => column.Id), Is.EqualTo(controller.GetStatusColumnIds()));
        Assert.That(board.StatusColumns[0].Title, Is.EqualTo("Backlog"));
    }

    [Test]
    public void CreateNoteRaisesContentChangedForItsColumn()
    {
        controller.CreateNote(progressId, "New note", "Australia");

        assertContentChangedFor(progressId);
    }

    [Test]
    public void UpdateNoteTitleRaisesContentChangedForItsColumn()
    {
        controller.UpdateNoteTitle(secondNoteId, "Renamed");

        assertContentChangedFor(plannedId);
    }

    [Test]
    public void UpdateNoteContentRaisesContentChangedForItsColumn()
    {
        controller.UpdateNoteContent(secondNoteId, "New content");

        assertContentChangedFor(plannedId);
    }

    [Test]
    public void TrashNoteRaisesContentChangedForItsColumn()
    {
        controller.TrashNote(firstNoteId);

        assertContentChangedFor(plannedId);
    }

    [Test]
    public void UpdateNoteFileRaisesContentChangedForItsColumn()
    {
        controller.UpdateNoteFile(firstNoteId, new FileReference {fullPath = Path.Combine(Path.GetTempPath(), "list.txt")});

        assertContentChangedFor(plannedId);
    }

    [Test]
    public void UpdateNoteRemoveFileRaisesContentChangedForItsColumn()
    {
        controller.UpdateNoteFile(firstNoteId, new FileReference {fullPath = Path.Combine(Path.GetTempPath(), "list.txt")});
        recorder.Clear();

        controller.DeleteNoteFile(firstNoteId);

        assertContentChangedFor(plannedId);
    }

    [Test]
    public void MoveNoteToColumnWithinColumnRaisesOneContentChanged()
    {
        controller.MoveNoteToColumn(firstNoteId, plannedId, 1);

        assertContentChangedFor(plannedId);
    }

    [Test]
    public void MoveNoteToColumnAcrossColumnsRaisesContentChangedForSourceThenTarget()
    {
        controller.MoveNoteToColumn(secondNoteId, progressId, 0);

        assertContentChangedFor(plannedId, progressId);
    }

    [Test]
    public void MoveColumnRaisesColumnOrderChangedWithNewOrderThenBoardChanged()
    {
        controller.MoveColumn(progressId, 0);

        Assert.That(recorder.Names, Is.EqualTo(new[] { nameof(BoardController.ColumnOrderChanged), nameof(BoardController.BoardChanged) }));
        var order = recorder.ArgsOf<ColumnOrderChangedEventArgs>().Single();
        Assert.That(order.ColumnIds, Is.EqualTo(new[] { progressId, plannedId }));
    }

    [Test]
    public void DeleteColumnRaisesColumnRemovedThenBoardChanged()
    {
        controller.DeleteColumn(progressId);

        Assert.That(recorder.Names, Is.EqualTo(new[] { nameof(BoardController.ColumnRemoved), nameof(BoardController.BoardChanged) }));
        Assert.That(recorder.ArgsOf<ColumnRemovedEventArgs>().Single().ColumnId, Is.EqualTo(progressId));
    }

    [Test]
    public void DeleteColumnWithMoveRaisesContentChangedForTargetThenColumnRemoved()
    {
        controller.DeleteColumn(plannedId, progressId);

        Assert.That(recorder.Names, Is.EqualTo(new[]
        {
            nameof(BoardController.ColumnContentChanged),
            nameof(BoardController.ColumnRemoved),
            nameof(BoardController.BoardChanged)
        }));
        var contentChanged = recorder.ArgsOf<ColumnContentChangedEventArgs>().Single();
        Assert.That(contentChanged.ColumnId, Is.EqualTo(progressId));
        Assert.That(contentChanged.NewNotes.Select(note => note.Id), Is.EqualTo(new[] { firstNoteId, secondNoteId }));
        Assert.That(recorder.ArgsOf<ColumnRemovedEventArgs>().Single().ColumnId, Is.EqualTo(plannedId));
    }

    [Test]
    public void OperationsWithoutSubscribersDoNotThrow()
    {
        var unobserved = new LocalBoardController();

        Assert.DoesNotThrow(() =>
        {
            Guid columnId = unobserved.CreateColumn("Planned");
            Guid otherColumnId = unobserved.CreateColumn("In Progress");
            Guid noteId = unobserved.CreateNote(columnId, "Note", "Content");
            unobserved.UpdateColumn(columnId, "Backlog");
            unobserved.UpdateNoteTitle(noteId, "Renamed");
            unobserved.UpdateNoteContent(noteId, "New content");
            unobserved.UpdateNoteFile(noteId, new FileReference{fullPath = Path.Combine(Path.GetTempPath(), "list.txt")});
            unobserved.DeleteNoteFile(noteId);
            unobserved.MoveNoteToColumn(noteId, otherColumnId, 0);
            unobserved.MoveColumn(otherColumnId, 0);
            unobserved.DeleteColumn(otherColumnId, columnId);
            unobserved.TrashNote(noteId);
            unobserved.DeleteColumn(columnId);
        });
    }
}
