namespace ToDo_App.Tests;

using ToDo_App.Controller;
using ToDo_App.Interfaces;

// Renaming and deleting a tag on all todos (#65)
public class TagRenameDeleteTests
{
    private BoardController controller;
    private EventRecorder recorder;
    private Guid plannedId;
    private Guid progressId;
    private Guid doneId;
    private Guid firstNoteId;
    private Guid secondNoteId;
    private Guid thirdNoteId;
    private Guid wrokId;
    private Guid jobId;

    [SetUp]
    public void Setup()
    {
        controller = new BoardController();
        plannedId = controller.CreateColumn("Planned");
        progressId = controller.CreateColumn("In Progress");
        doneId = controller.CreateColumn("Done");
        firstNoteId = controller.CreateNote(plannedId, "First", "", tagNames: new[] { "Wrok", "Job" });
        secondNoteId = controller.CreateNote(plannedId, "Second", "", tagNames: new[] { "Wrok" });
        thirdNoteId = controller.CreateNote(progressId, "Third", "", tagNames: new[] { "Wrok" });
        controller.CreateNote(doneId, "Untagged", "");
        wrokId = tagId("Wrok");
        jobId = tagId("Job");
        recorder = new EventRecorder(controller);
    }

    private Guid tagId(string name) => controller.GetTags().Single(tag => tag.Name == name).Id;

    private ToDoNote note(Guid noteId) =>
        controller.GetStatusColumnIds().SelectMany(controller.GetNotesInColumn).Single(note => note.Id == noteId);

    private List<string> tagNames(Guid noteId) => note(noteId).Tags.Select(tag => tag.Name).ToList();

    // --- Rename ---

    [Test]
    public void RenameTagChangesItOnEveryTodo()
    {
        controller.RenameTag(wrokId, "Work");

        Assert.That(tagNames(firstNoteId), Is.EqualTo(new[] { "Work", "Job" }));
        Assert.That(tagNames(secondNoteId), Is.EqualTo(new[] { "Work" }));
        Assert.That(tagNames(thirdNoteId), Is.EqualTo(new[] { "Work" }));
    }

    [Test]
    public void RenameTagKeepsItsId()
    {
        controller.RenameTag(wrokId, "Work");

        Assert.That(tagId("Work"), Is.EqualTo(wrokId));
    }

    [Test]
    public void RenameTagTrimsTheName()
    {
        controller.RenameTag(wrokId, "  Work ");

        Assert.That(tagNames(secondNoteId), Is.EqualTo(new[] { "Work" }));
    }

    [Test]
    public void RenameToAnotherTagsNameIsBlockedAndChangesNothing()
    {
        Assert.Throws<ArgumentException>(() => controller.RenameTag(jobId, "wrok"));

        Assert.That(tagNames(firstNoteId), Is.EqualTo(new[] { "Wrok", "Job" }));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void ChangingOnlyTheCaseIsAllowed()
    {
        controller.RenameTag(wrokId, "WROK");

        Assert.That(tagNames(secondNoteId), Is.EqualTo(new[] { "WROK" }));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("ThirteenChars")]
    public void RenameToInvalidNameIsBlockedAndChangesNothing(string invalidName)
    {
        Assert.Throws<ArgumentException>(() => controller.RenameTag(wrokId, invalidName));

        Assert.That(tagNames(secondNoteId), Is.EqualTo(new[] { "Wrok" }));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void RenameUnknownTagThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.RenameTag(Guid.NewGuid(), "Work"));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void RenameRaisesTagChangedThenBoardChanged()
    {
        controller.RenameTag(wrokId, "Work");

        Assert.That(recorder.Names, Is.EqualTo(new[]
        {
            nameof(BoardController.TagChanged),
            nameof(BoardController.BoardChanged)
        }));
        var changed = recorder.ArgsOf<TagChangedEventArgs>().Single();
        Assert.That(changed.TagId, Is.EqualTo(wrokId));
        Assert.That(changed.Name, Is.EqualTo("Work"));
    }

    // --- Delete ---

    [Test]
    public void DeleteTagRemovesItFromEveryTodoAndKeepsTheTodos()
    {
        controller.DeleteTag(wrokId);

        Assert.That(tagNames(firstNoteId), Is.EqualTo(new[] { "Job" }));
        Assert.That(note(secondNoteId).Tags, Is.Empty);
        Assert.That(note(thirdNoteId).Tags, Is.Empty);
        Assert.That(controller.GetTags().Select(tag => tag.Name), Is.EqualTo(new[] { "Job" }));
    }

    [Test]
    public void DeleteUnknownTagThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.DeleteTag(Guid.NewGuid()));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void DeleteRaisesContentChangedForEachAffectedColumnThenBoardChanged()
    {
        controller.DeleteTag(wrokId);

        Assert.That(recorder.Names, Is.EqualTo(new[]
        {
            nameof(BoardController.ColumnContentChanged),
            nameof(BoardController.ColumnContentChanged),
            nameof(BoardController.BoardChanged)
        }));
        Assert.That(recorder.ArgsOf<ColumnContentChangedEventArgs>().Select(e => e.ColumnId),
            Is.EqualTo(new[] { plannedId, progressId }));
    }

    // --- Counting ---

    [Test]
    public void CountNotesWithTagCountsTodos()
    {
        Assert.That(controller.CountNotesWithTag(wrokId), Is.EqualTo(3));
        Assert.That(controller.CountNotesWithTag(jobId), Is.EqualTo(1));
        Assert.That(controller.CountNotesWithTag(Guid.NewGuid()), Is.Zero);
    }
}

// Rename and delete are saved in board.json (#65)
public class TagRenameDeletePersistenceTests
{
    private string jsonPath;

    [SetUp]
    public void Setup()
    {
        jsonPath = Path.Combine(Path.GetTempPath(), $"todo-tests-{Guid.NewGuid()}.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(jsonPath))
        {
            File.Delete(jsonPath);
        }
    }

    [Test]
    public void RenameAndDeleteAreKeptAfterReload()
    {
        var original = new BoardController();
        Guid columnId = original.CreateColumn("Planned");
        original.CreateNote(columnId, "First", "", tagNames: new[] { "Wrok", "Old" });
        original.CreateNote(columnId, "Second", "", tagNames: new[] { "Wrok" });
        original.RenameTag(original.GetTags().Single(tag => tag.Name == "Wrok").Id, "Work");
        original.DeleteTag(original.GetTags().Single(tag => tag.Name == "Old").Id);
        original.SaveBoardToJson(jsonPath);

        var loaded = new BoardController();
        loaded.LoadBoardFromJson(jsonPath);

        var notes = loaded.GetNotesInColumn(columnId);
        Assert.That(notes.Select(n => n.Tags.Select(tag => tag.Name)), Is.EqualTo(new[] { new[] { "Work" }, new[] { "Work" } }));
    }
}
