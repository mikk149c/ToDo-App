namespace ToDo_App.Tests;

using ToDo_App.Controller;

// BoardController behaviour (#59, #135)
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
    public void UpdateColumnChangesTitle()
    {
        controller.UpdateColumn(plannedId, "Backlog");

        Assert.That(controller.GetStatusColumnName(plannedId), Is.EqualTo("Backlog"));
    }

    [Test]
    public void UpdateColumnWithInvalidIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.UpdateColumn(Guid.NewGuid(), "Backlog"));
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
        var file = new AttachedFile(Guid.NewGuid(), "list.txt", Path.Combine(Path.GetTempPath(), "list.txt"));

        Guid createdId = controller.CreateNote(progressId, "Shopping", "Køb bananer", file);

        var note = controller.GetNotesInColumn(progressId).Single();
        Assert.That(note.Id, Is.EqualTo(createdId));
        Assert.That(note.Title, Is.EqualTo("Shopping"));
        Assert.That(note.Content, Is.EqualTo("Køb bananer"));
        Assert.That(note.File, Is.SameAs(file));
    }

    [Test]
    public void UpdateNoteTitleAndContentChangeTheNote()
    {
        controller.UpdateNoteTitle(noteId, "Renamed");
        controller.UpdateNoteContent(noteId, "New content");

        var note = controller.GetNotesInColumn(plannedId).Single();
        Assert.That(note.Title, Is.EqualTo("Renamed"));
        Assert.That(note.Content, Is.EqualTo("New content"));
    }

    [Test]
    public void TrashNoteRemovesTheNote()
    {
        controller.TrashNote(noteId);

        Assert.That(controller.GetNotesInColumn(plannedId), Is.Empty);
    }

    [Test]
    public void UpdateNoteFileStoresTheFile()
    {
        var fileReference = new FileReference
        {
            fullPath = Path.Combine(Path.GetTempPath(), "attachments", "shopping list.txt")
        };

        controller.UpdateNoteFile(noteId, fileReference);

        Assert.That(controller.GetNotesInColumn(plannedId).Single().File.Name, Is.SameAs(fileReference.name));
        Assert.That(controller.GetNotesInColumn(plannedId).Single().File.FullPath, Is.SameAs(fileReference.fullPath));
    }

    // Decision: the path isn't checked. The UI picks an existing file, and an attached file can be deleted
    // later anyway, so rejecting missing files would be a new feature. The attachment is stored as given.
    [Test]
    public void UpdateNoteFileWithNonExistentPathStoresIt()
    {
        string name = "missing.txt";
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), name);
        Assume.That(File.Exists(path), Is.False);

        controller.UpdateNoteFile(noteId, new FileReference
        {
            name = name,
            fullPath = path
        });

        var file = controller.GetNotesInColumn(plannedId).Single().File;
        Assert.That(file!.Name, Is.EqualTo("missing.txt"));
        Assert.That(file.FullPath, Is.EqualTo(path));
    }

    [Test]
    public void DeleteNoteFileClearsIt()
    {
        controller.UpdateNoteFile(noteId,
            new FileReference
            {
                fullPath = Path.Combine(Path.GetTempPath(), "list.txt")
            }
        );

        controller.DeleteNoteFile(noteId);

        Assert.That(controller.GetNotesInColumn(plannedId).Single().File, Is.Null);
    }

    // Unknown note Ids throw KeyNotFoundException, like unknown column Ids, and change nothing
    [Test]
    public void UpdateNoteTitleWithUnknownIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.UpdateNoteTitle(Guid.NewGuid(), "Renamed"));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void DeleteNoteFileWithUnknownIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.DeleteNoteFile(Guid.NewGuid()));
        Assert.That(recorder.Names, Is.Empty);
    }

    // Each change only touches its own part of the note, so two people editing different parts don't overwrite each other
    [Test]
    public void UpdateNoteTitleLeavesTheRestOfTheNoteAlone()
    {
        var fileReference = new FileReference
        {
            fullPath = Path.Combine(Path.GetTempPath(), "list.txt")
        };
        controller.UpdateNoteTags(noteId, new[] { "Work" });
        controller.UpdateNoteFile(noteId, fileReference);

        controller.UpdateNoteTitle(noteId, "Renamed");

        var note = controller.GetNotesInColumn(plannedId).Single();
        Assert.That(note.Title, Is.EqualTo("Renamed"));
        Assert.That(note.Content, Is.EqualTo("Køb bananer"));
        Assert.That(note.Tags.Select(tag => tag.Name), Is.EqualTo(new[] { "Work" }));
        Assert.That(note.File.Name, Is.SameAs(fileReference.name));
        Assert.That(note.File.FullPath, Is.SameAs(fileReference.fullPath));
    }

    [Test]
    public void DeleteColumnWithInvalidIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.DeleteColumn(Guid.NewGuid()));
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
