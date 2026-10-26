namespace ToDo_App.Tests;

using ToDo_App.Controller;
using ToDo_App.Interfaces;

// Filtering the board by one or more tags (#67, #69)
public class TagFilterTests
{
    private BoardController controller;
    private TagFilter filter;
    private Guid columnId;
    private Guid workId;
    private Guid homeId;

    [SetUp]
    public void Setup()
    {
        controller = new LocalBoardController();
        columnId = controller.CreateColumn("Planned");
        controller.CreateNote(columnId, "Work", "", tagNames: new[] { "Work" });
        controller.CreateNote(columnId, "Home", "", tagNames: new[] { "Home" });
        controller.CreateNote(columnId, "Both", "", tagNames: new[] { "Work", "Home" });
        controller.CreateNote(columnId, "Untagged", "");
        controller.CreateNote(columnId, "Garden", "", tagNames: new[] { "Garden" });
        workId = tagId("Work");
        homeId = tagId("Home");
        filter = new TagFilter();
    }

    private Guid tagId(string name) => controller.GetTags().Single(tag => tag.Name == name).Id;

    private List<ToDoNote> notes() => controller.GetNotesInColumn(columnId);

    private List<string> shownTitles() => filter.Apply(notes()).Select(note => note.Title).ToList();

    // --- One tag ---

    [Test]
    public void WithoutAFilterEveryTodoIsShown()
    {
        Assert.That(filter.IsActive, Is.False);
        Assert.That(shownTitles(), Is.EqualTo(new[] { "Work", "Home", "Both", "Untagged", "Garden" }));
    }

    [Test]
    public void FilteringByATagShowsOnlyTodosWithIt()
    {
        filter.Select(workId);

        Assert.That(filter.IsActive, Is.True);
        Assert.That(shownTitles(), Is.EqualTo(new[] { "Work", "Both" }));
    }

    [Test]
    public void ClearingTheFilterShowsEverythingAgainInTheOriginalOrder()
    {
        filter.Select(workId);
        filter.Clear();

        Assert.That(filter.IsActive, Is.False);
        Assert.That(shownTitles(), Is.EqualTo(new[] { "Work", "Home", "Both", "Untagged", "Garden" }));
    }

    [Test]
    public void FilteringNeverChangesTheBoard()
    {
        filter.Select(workId);
        filter.Apply(notes());

        Assert.That(notes(), Has.Count.EqualTo(5));
    }

    [Test]
    public void SelectingTheSameTagTwiceKeepsItOnce()
    {
        filter.Select(workId);
        filter.Select(workId);

        Assert.That(filter.TagIds, Is.EqualTo(new[] { workId }));
    }

    [Test]
    public void DeselectingTheLastTagTurnsTheFilterOff()
    {
        filter.Select(workId);
        filter.Deselect(workId);

        Assert.That(filter.IsActive, Is.False);
    }

    [Test]
    public void RenamingTheFilteredTagKeepsTheFilterOn()
    {
        filter.Select(workId);

        controller.UpdateTag(workId, name: "Job");

        Assert.That(filter.Prune(controller.GetTags()), Is.False);
        Assert.That(shownTitles(), Is.EqualTo(new[] { "Work", "Both" }));
    }

    [Test]
    public void TodoThatLosesTheFilteredTagIsHiddenButStillExists()
    {
        filter.Select(workId);
        Guid workNoteId = notes().Single(note => note.Title == "Work").Id;

        controller.UpdateNote(workNoteId, new NoteChanges { TagNames = new[] { "Garden" } });

        Assert.That(shownTitles(), Is.EqualTo(new[] { "Both" }));
        Assert.That(notes().Select(note => note.Title), Does.Contain("Work"));
    }

    [Test]
    public void FilterTurnsOffWhenTheLastTodoWithTheTagLosesIt()
    {
        filter.Select(tagId("Garden"));
        controller.DeleteTag(tagId("Garden"));

        Assert.That(filter.Prune(controller.GetTags()), Is.True);
        Assert.That(filter.IsActive, Is.False);
        Assert.That(shownTitles(), Has.Count.EqualTo(5));
    }

    // --- Several tags ---

    [Test]
    public void AnyModeShowsTodosWithAtLeastOneSelectedTag()
    {
        filter.Select(workId);
        filter.Select(homeId);
        filter.Mode = TagMatchMode.Any;

        Assert.That(shownTitles(), Is.EqualTo(new[] { "Work", "Home", "Both" }));
    }

    [Test]
    public void AllModeShowsOnlyTodosWithEverySelectedTag()
    {
        filter.Select(workId);
        filter.Select(homeId);
        filter.Mode = TagMatchMode.All;

        Assert.That(shownTitles(), Is.EqualTo(new[] { "Both" }));
    }

    [Test]
    public void ModeDefaultsToAny()
    {
        Assert.That(filter.Mode, Is.EqualTo(TagMatchMode.Any));
    }

    [Test]
    public void PruneKeepsTagsThatStillExistAndDropsTheOthers()
    {
        filter.Select(workId);
        filter.Select(tagId("Garden"));
        controller.DeleteTag(tagId("Garden"));

        Assert.That(filter.Prune(controller.GetTags()), Is.True);
        Assert.That(filter.TagIds, Is.EqualTo(new[] { workId }));
    }

    // --- Positions while filtered ---

    [Test]
    public void ColumnIndexIsUnchangedWithoutAFilter()
    {
        Assert.That(filter.ToColumnIndex(notes(), 3), Is.EqualTo(3));
    }

    [TestCase(0, 0)] // before "Work"
    [TestCase(1, 2)] // before "Both", so after the hidden "Home"
    [TestCase(2, 3)] // after "Both"
    public void ColumnIndexSkipsHiddenTodos(int shownIndex, int expected)
    {
        filter.Select(workId);

        Assert.That(filter.ToColumnIndex(notes(), shownIndex), Is.EqualTo(expected));
    }

    [Test]
    public void ColumnIndexIsTheEndWhenNothingIsShown()
    {
        filter.Select(Guid.NewGuid()); // no todo in this column has it

        Assert.That(filter.ToColumnIndex(notes(), 0), Is.EqualTo(notes().Count));
    }

    [Test]
    public void DroppingWhileFilteredKeepsHiddenTodosWhereTheyAre()
    {
        filter.Select(workId);
        Guid bothId = notes().Single(note => note.Title == "Both").Id;
        int index = filter.ToColumnIndex(notes(), 0); // top of the shown todos

        controller.MoveNoteToColumn(bothId, columnId, index);

        Assert.That(notes().Select(note => note.Title),
            Is.EqualTo(new[] { "Both", "Work", "Home", "Untagged", "Garden" }));
    }
}
