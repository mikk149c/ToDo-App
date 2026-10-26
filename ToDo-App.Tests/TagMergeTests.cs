namespace ToDo_App.Tests;

using ToDo_App.Controller;
using ToDo_App.Interfaces;

// Merging two tags (#68)
public class TagMergeTests
{
    private BoardController controller;
    private EventRecorder recorder;
    private Guid plannedId;
    private Guid progressId;
    private Guid doneId;
    private Guid jobOnlyNoteId;
    private Guid workOnlyNoteId;
    private Guid bothNoteId;
    private Guid untaggedNoteId;
    private Guid jobId;
    private Guid workId;

    [SetUp]
    public void Setup()
    {
        controller = new LocalBoardController();
        plannedId = controller.CreateColumn("Planned");
        progressId = controller.CreateColumn("In Progress");
        doneId = controller.CreateColumn("Done");
        jobOnlyNoteId = controller.CreateNote(plannedId, "Job only", "", tagNames: new[] { "Urgent", "Job" });
        workOnlyNoteId = controller.CreateNote(progressId, "Work only", "", tagNames: new[] { "Work" });
        bothNoteId = controller.CreateNote(plannedId, "Both", "", tagNames: new[] { "Job", "Work" });
        untaggedNoteId = controller.CreateNote(doneId, "Untagged", "");
        jobId = tagId("Job");
        workId = tagId("Work");
        controller.UpdateTag(workId, color: "#0F6CBD");
        recorder = new EventRecorder(controller);
    }

    private Guid tagId(string name) => controller.GetTags().Single(tag => tag.Name == name).Id;

    private ToDoNote note(Guid noteId) =>
        controller.GetStatusColumnIds().SelectMany(controller.GetNotesInColumn).Single(note => note.Id == noteId);

    private List<string> tagNames(Guid noteId) => note(noteId).Tags.Select(tag => tag.Name).ToList();

    [Test]
    public void EveryTodoThatHadEitherTagHasTheTargetTag()
    {
        controller.MergeTags(jobId, workId);

        Assert.That(tagNames(jobOnlyNoteId), Is.EqualTo(new[] { "Urgent", "Work" }));
        Assert.That(tagNames(workOnlyNoteId), Is.EqualTo(new[] { "Work" }));
        Assert.That(note(untaggedNoteId).Tags, Is.Empty);
    }

    [Test]
    public void ATodoWithBothTagsHasTheTargetExactlyOnce()
    {
        controller.MergeTags(jobId, workId);

        Assert.That(tagNames(bothNoteId), Is.EqualTo(new[] { "Work" }));
    }

    [Test]
    public void TheMergedTagNoLongerExists()
    {
        controller.MergeTags(jobId, workId);

        Assert.That(controller.GetTags().Select(tag => tag.Id), Does.Not.Contain(jobId));
    }

    [Test]
    public void TheTargetKeepsItsIdNameAndColour()
    {
        controller.MergeTags(jobId, workId);

        NoteTag merged = note(jobOnlyNoteId).Tags.Single(tag => tag.Name == "Work");
        Assert.That(merged.Id, Is.EqualTo(workId));
        Assert.That(merged.Color, Is.EqualTo("#0F6CBD"));
    }

    [Test]
    public void MergeRaisesColumnContentChangedForEveryAffectedColumnThenOneBoardChanged()
    {
        controller.MergeTags(jobId, workId);

        Assert.That(recorder.Names, Is.EqualTo(new[]
        {
            nameof(BoardController.ColumnContentChanged),
            nameof(BoardController.BoardChanged)
        }));
        Assert.That(recorder.ArgsOf<ColumnContentChangedEventArgs>().Single().ColumnId, Is.EqualTo(plannedId));
    }

    [Test]
    public void MergeIsKeptWhenTheBoardIsSavedAndLoaded()
    {
        string jsonPath = Path.Combine(Path.GetTempPath(), $"todo-tests-{Guid.NewGuid()}.json");
        try
        {
            controller.MergeTags(jobId, workId);
            controller.SaveBoardToJson(jsonPath);

            var reloaded = new LocalBoardController();
            reloaded.LoadBoardFromJson(jsonPath);

            Assert.That(reloaded.GetTags().Select(tag => tag.Name), Is.EqualTo(new[] { "Urgent", "Work" }));
        }
        finally
        {
            File.Delete(jsonPath);
        }
    }

    [Test]
    public void MergingATagIntoItselfThrowsWithoutEvents()
    {
        Assert.Throws<ArgumentException>(() => controller.MergeTags(jobId, jobId));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void MergingUnknownTagsThrowsWithoutEventsAndChangesNothing()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.MergeTags(Guid.NewGuid(), workId));
        Assert.Throws<KeyNotFoundException>(() => controller.MergeTags(jobId, Guid.NewGuid()));

        Assert.That(tagNames(jobOnlyNoteId), Is.EqualTo(new[] { "Urgent", "Job" }));
        Assert.That(recorder.Names, Is.Empty);
    }
}
