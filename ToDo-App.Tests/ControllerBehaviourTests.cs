namespace ToDo_App.Tests;

using ToDo_App.Controller;

// BoardController behaviour (#59).
// MoveColumnLeft/MoveColumnRight are deliberately not tested: they throw NotImplementedException,
// and a test would only pin an unfinished stub.
public class ControllerBehaviourTests
{
    private BoardController controller;
    private EventRecorder recorder;
    private Guid plannedId;
    private Guid progressId;
    private Guid doneId;
    private Guid noteId;

    [SetUp]
    public void Setup()
    {
        controller = new LocalBoardController();
        plannedId = controller.CreateColumn("Planned");
        progressId = controller.CreateColumn("In Progress");
        doneId = controller.CreateColumn("Done");
        noteId = controller.CreateNote(plannedId, "First note", "Køb bananer");
        recorder = new EventRecorder(controller);
    }

    [Test]
    public void GetStatusColumnIdsReturnsIdsInBoardOrder()
    {
        Assert.That(controller.GetStatusColumnIds(), Is.EqualTo(new[] { plannedId, progressId, doneId }));
    }

    [Test]
    public void GetStatusColumnIdsReflectsMoveColumn()
    {
        controller.MoveColumn(doneId, 0);

        Assert.That(controller.GetStatusColumnIds(), Is.EqualTo(new[] { doneId, plannedId, progressId }));
    }

    [Test]
    public void GetStatusColumnNameReturnsTitle()
    {
        Assert.That(controller.GetStatusColumnName(progressId), Is.EqualTo("In Progress"));
    }

    [Test]
    public void GetStatusColumnNameWithInvalidIdThrows()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.GetStatusColumnName(Guid.NewGuid()));
    }

    [Test]
    public void GetNotesInColumnWithInvalidIdThrows()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.GetNotesInColumn(Guid.NewGuid()));
    }

    [Test]
    public void SetStatusColumnNameChangesTitle()
    {
        controller.SetStatusColumnName(plannedId, "Backlog");

        Assert.That(controller.GetStatusColumnName(plannedId), Is.EqualTo("Backlog"));
    }

    [Test]
    public void SetStatusColumnNameWithInvalidIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.SetStatusColumnName(Guid.NewGuid(), "Backlog"));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void CreateNoteInInvalidColumnThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.CreateNote(Guid.NewGuid(), "Note", "Content"));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void CreateNoteStoresTitleContentAndFile()
    {
        var file = new AttachedFile("list.txt", Path.Combine(Path.GetTempPath(), "list.txt"));

        Guid createdId = controller.CreateNote(progressId, "Shopping", "Køb bananer", file);

        var note = controller.GetNotesInColumn(progressId).Single();
        Assert.That(note.Id, Is.EqualTo(createdId));
        Assert.That(note.Title, Is.EqualTo("Shopping"));
        Assert.That(note.Content, Is.EqualTo("Køb bananer"));
        Assert.That(note.File, Is.SameAs(file));
    }

    [Test]
    public void SetNoteTitleAndContentChangeTheNote()
    {
        controller.SetNoteTitle(noteId, "Renamed");
        controller.SetNoteContent(noteId, "New content");

        var note = controller.GetNotesInColumn(plannedId).Single();
        Assert.That(note.Title, Is.EqualTo("Renamed"));
        Assert.That(note.Content, Is.EqualTo("New content"));
    }

    [Test]
    public void RemoveNoteRemovesTheNote()
    {
        controller.RemoveNote(noteId);

        Assert.That(controller.GetNotesInColumn(plannedId), Is.Empty);
    }

    [Test]
    public void SetNoteFileAttachmentTakesNameFromPathAndKeepsFullPath()
    {
        string path = Path.Combine(Path.GetTempPath(), "attachments", "shopping list.txt");

        controller.SetNoteFileAttachment(noteId, path);

        var file = controller.GetNotesInColumn(plannedId).Single().File;
        Assert.That(file, Is.Not.Null);
        Assert.That(file!.Name, Is.EqualTo("shopping list.txt"));
        Assert.That(file.FullPath, Is.EqualTo(path));
    }

    // Decision: the path isn't checked. The UI picks an existing file, and an attached file can be deleted
    // later anyway, so rejecting missing files would be a new feature. The attachment is stored as given.
    [Test]
    public void SetNoteFileAttachmentWithNonExistentPathStoresIt()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.txt");
        Assume.That(File.Exists(path), Is.False);

        controller.SetNoteFileAttachment(noteId, path);

        var file = controller.GetNotesInColumn(plannedId).Single().File;
        Assert.That(file!.Name, Is.EqualTo("missing.txt"));
        Assert.That(file.FullPath, Is.EqualTo(path));
    }

    [Test]
    public void RemoveNoteFileAttachmentClearsIt()
    {
        controller.SetNoteFileAttachment(noteId, Path.Combine(Path.GetTempPath(), "list.txt"));

        controller.RemoveNoteFileAttachment(noteId);

        Assert.That(controller.GetNotesInColumn(plannedId).Single().File, Is.Null);
    }

    // Unknown note Ids throw KeyNotFoundException, like unknown column Ids, and change nothing.
    // RemoveNote and the moves don't do this yet; see KnownBugTests (#61).
    [Test]
    public void SetNoteTitleWithUnknownIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.SetNoteTitle(Guid.NewGuid(), "Renamed"));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void SetNoteContentWithUnknownIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.SetNoteContent(Guid.NewGuid(), "New content"));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void SetNoteFileAttachmentWithUnknownIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            controller.SetNoteFileAttachment(Guid.NewGuid(), Path.Combine(Path.GetTempPath(), "list.txt")));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void RemoveNoteFileAttachmentWithUnknownIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.RemoveNoteFileAttachment(Guid.NewGuid()));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void MoveNoteByIndexMovesTheNote()
    {
        controller.MoveNote(0, 0, 2, 0);

        Assert.That(controller.GetNotesInColumn(plannedId), Is.Empty);
        Assert.That(controller.GetNotesInColumn(doneId).Single().Id, Is.EqualTo(noteId));
    }

    [Test]
    public void RemoveColumnWithInvalidIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.RemoveColumn(Guid.NewGuid()));
        Assert.That(recorder.Names, Is.Empty);
        Assert.That(controller.GetStatusColumnIds(), Has.Count.EqualTo(3));
    }

    [Test]
    public void MoveColumnWithInvalidIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.MoveColumn(Guid.NewGuid(), 0));
        Assert.That(recorder.Names, Is.Empty);
    }
}
