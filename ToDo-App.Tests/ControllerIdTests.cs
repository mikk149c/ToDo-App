namespace ToDo_App.Tests;

using ToDo_App.Controller;

// The parent controller makes the Ids for new columns, notes and tags before a change goes anywhere, so a remote
// change gives them the same Id on every copy of the board
public class ControllerIdTests
{
    // A local controller that remembers the Ids it was given
    private sealed class RecordingController : LocalBoardController
    {
        public Guid ColumnId { get; private set; }
        public Guid NoteId { get; private set; }
        public IReadOnlyDictionary<string, Guid>? NewTagIds { get; private set; }

        protected override void CreateColumn(Guid columnId, string name)
        {
            ColumnId = columnId;
            base.CreateColumn(columnId, name);
        }

        protected override void CreateNote(Guid columnId, Guid noteId, string title, string content, AttachedFile? file,
            IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors, IReadOnlyDictionary<string, Guid> newTagIds)
        {
            NoteId = noteId;
            NewTagIds = newTagIds;
            base.CreateNote(columnId, noteId, title, content, file, tagNames, newTagColors, newTagIds);
        }
    }

    [Test]
    public void NewColumnsAndNotesGetTheIdsThatAreReturned()
    {
        var controller = new RecordingController();

        Guid columnId = controller.CreateColumn("Todo");
        Guid noteId = controller.CreateNote(columnId, "Note", "");

        Assert.That(controller.ColumnId, Is.EqualTo(columnId));
        Assert.That(controller.NoteId, Is.EqualTo(noteId));
        Assert.That(controller.GetNotesInColumn(columnId).Single().Id, Is.EqualTo(noteId));
    }

    [Test]
    public void NewTagsGetTheIdsChosenByTheController()
    {
        var controller = new RecordingController();
        Guid columnId = controller.CreateColumn("Todo");

        controller.CreateNote(columnId, "Note", "", tagNames: new[] { " Work ", "work", "Home" });

        Assert.That(controller.NewTagIds!.Keys, Is.EquivalentTo(new[] { "Work", "Home" }));
        Assert.That(controller.GetTags().Single(tag => tag.Name == "Work").Id, Is.EqualTo(controller.NewTagIds!["Work"]));
    }

    [Test]
    public void AnExistingTagKeepsItsId()
    {
        var controller = new LocalBoardController();
        Guid columnId = controller.CreateColumn("Todo");
        controller.CreateNote(columnId, "First", "", tagNames: new[] { "Work" });
        Guid workId = controller.GetTags().Single().Id;

        Guid secondNoteId = controller.CreateNote(columnId, "Second", "", tagNames: new[] { "work" });

        Assert.That(controller.GetNotesInColumn(columnId).Single(note => note.Id == secondNoteId).Tags.Single().Id, Is.EqualTo(workId));
        Assert.That(controller.GetTags(), Has.Count.EqualTo(1));
    }
}
