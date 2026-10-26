namespace ToDo_App.Tests;

using ToDo_App.Controller;
using ToDo_App.Interfaces;

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
        var file = new AttachedFile("list.txt", Path.Combine(Path.GetTempPath(), "list.txt"));

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
        controller.UpdateNote(noteId, new NoteChanges { Title = "Renamed", Content = "New content" });

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
        var file = new AttachedFile("shopping list.txt", Path.Combine(Path.GetTempPath(), "attachments", "shopping list.txt"));

        controller.UpdateNote(noteId, new NoteChanges { File = file });

        Assert.That(controller.GetNotesInColumn(plannedId).Single().File, Is.SameAs(file));
    }

    // Decision: the path isn't checked. The UI picks an existing file, and an attached file can be deleted
    // later anyway, so rejecting missing files would be a new feature. The attachment is stored as given.
    [Test]
    public void UpdateNoteFileWithNonExistentPathStoresIt()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.txt");
        Assume.That(File.Exists(path), Is.False);

        controller.UpdateNote(noteId, new NoteChanges { File = new AttachedFile("missing.txt", path) });

        var file = controller.GetNotesInColumn(plannedId).Single().File;
        Assert.That(file!.Name, Is.EqualTo("missing.txt"));
        Assert.That(file.FullPath, Is.EqualTo(path));
    }

    [Test]
    public void UpdateNoteRemoveFileClearsIt()
    {
        controller.UpdateNote(noteId, new NoteChanges { File = new AttachedFile("list.txt", Path.Combine(Path.GetTempPath(), "list.txt")) });

        controller.UpdateNote(noteId, new NoteChanges { RemoveFile = true });

        Assert.That(controller.GetNotesInColumn(plannedId).Single().File, Is.Null);
    }

    // Unknown note Ids throw KeyNotFoundException, like unknown column Ids, and change nothing
    [Test]
    public void UpdateNoteWithUnknownIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.UpdateNote(Guid.NewGuid(), new NoteChanges { Title = "Renamed" }));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void UpdateNoteRemoveFileWithUnknownIdThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.UpdateNote(Guid.NewGuid(), new NoteChanges { RemoveFile = true }));
        Assert.That(recorder.Names, Is.Empty);
    }

    // Only the fields that are set change, so two people editing different parts of a note don't overwrite each other
    [Test]
    public void UpdateNoteLeavesFieldsThatAreNotSetAlone()
    {
        var file = new AttachedFile("list.txt", Path.Combine(Path.GetTempPath(), "list.txt"));
        controller.UpdateNote(noteId, new NoteChanges { TagNames = new[] { "Work" }, File = file });

        controller.UpdateNote(noteId, new NoteChanges { Title = "Renamed" });

        var note = controller.GetNotesInColumn(plannedId).Single();
        Assert.That(note.Title, Is.EqualTo("Renamed"));
        Assert.That(note.Content, Is.EqualTo("Køb bananer"));
        Assert.That(note.Tags.Select(tag => tag.Name), Is.EqualTo(new[] { "Work" }));
        Assert.That(note.File, Is.SameAs(file));
    }

    [Test]
    public void UpdateNoteWithAnInvalidTagChangesNothing()
    {
        Assert.Throws<ArgumentException>(() =>
            controller.UpdateNote(noteId, new NoteChanges { Title = "Renamed", TagNames = new[] { "   " } }));

        Assert.That(controller.GetNotesInColumn(plannedId).Single().Title, Is.EqualTo("First note"));
        Assert.That(recorder.Names, Is.Empty);
    }

    // An edit dialog sends only what the user changed, so a change made meanwhile by someone else isn't undone
    [Test]
    public void NoteChangesSinceOpeningKeepsOnlyWhatTheDialogChanged()
    {
        var opened = OpenedNote.Of(controller.GetNotesInColumn(plannedId).Single());
        controller.UpdateNote(noteId, new NoteChanges { Content = "Changed by someone else" });

        NoteChanges changes = NoteChanges.Since(opened, "Renamed", "Køb bananer", Array.Empty<string>(), null);
        controller.UpdateNote(noteId, changes);

        Assert.That(changes, Is.EqualTo(new NoteChanges { Title = "Renamed" }));
        var note = controller.GetNotesInColumn(plannedId).Single();
        Assert.That(note.Title, Is.EqualTo("Renamed"));
        Assert.That(note.Content, Is.EqualTo("Changed by someone else"));
    }

    [Test]
    public void NoteChangesSinceOpeningWithoutEditsHasNoChanges()
    {
        controller.UpdateNote(noteId, new NoteChanges { TagNames = new[] { "Work" } });
        var opened = OpenedNote.Of(controller.GetNotesInColumn(plannedId).Single());

        NoteChanges changes = NoteChanges.Since(opened, "First note", "Køb bananer", new[] { "Work" }, null);

        Assert.That(changes.HasChanges(), Is.False);
    }

    // Renaming a tag changes it on the note in place, so the opened tag names must be a copy
    [Test]
    public void OpenedNoteKeepsTheTagNamesItWasOpenedWith()
    {
        controller.UpdateNote(noteId, new NoteChanges { TagNames = new[] { "Work" } });
        var opened = OpenedNote.Of(controller.GetNotesInColumn(plannedId).Single());

        controller.UpdateTag(controller.GetTags().Single().Id, name: "Job");

        Assert.That(opened.TagNames, Is.EqualTo(new[] { "Work" }));
    }

    [Test]
    public void UpdateNoteWithFileAndRemoveFileThrowsWithoutChanges()
    {
        Assert.Throws<ArgumentException>(() =>
            controller.UpdateNote(noteId, new NoteChanges { File = new AttachedFile("list.txt", Path.Combine(Path.GetTempPath(), "list.txt")), RemoveFile = true }));

        Assert.That(controller.GetNotesInColumn(plannedId).Single().File, Is.Null);
        Assert.That(recorder.Names, Is.Empty);
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
