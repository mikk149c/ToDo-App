namespace ToDo_App.Tests;

using ToDo_App.Controller;
using ToDo_App.Interfaces;

// Choosing a colour for a tag (#66)
public class TagColorTests
{
    private const string Blue = "#0F6CBD";

    private BoardController controller;
    private EventRecorder recorder;
    private Guid plannedId;
    private Guid progressId;
    private Guid firstNoteId;
    private Guid secondNoteId;
    private Guid thirdNoteId;
    private Guid workId;
    private Guid homeId;
    private string jsonPath;

    [SetUp]
    public void Setup()
    {
        controller = new LocalBoardController();
        plannedId = controller.CreateColumn("Planned");
        progressId = controller.CreateColumn("In Progress");
        firstNoteId = controller.CreateNote(plannedId, "First", "", tagNames: new[] { "Work", "Home" });
        secondNoteId = controller.CreateNote(progressId, "Second", "", tagNames: new[] { "Work" });
        thirdNoteId = controller.CreateNote(progressId, "Third", "");
        workId = tagId("Work");
        homeId = tagId("Home");
        recorder = new EventRecorder(controller);
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

    private Guid tagId(string name) => controller.GetTags().Single(tag => tag.Name == name).Id;

    private ToDoNote note(Guid noteId) =>
        controller.GetStatusColumnIds().SelectMany(controller.GetNotesInColumn).Single(note => note.Id == noteId);

    private string? colorOf(Guid noteId, string tagName) => note(noteId).Tags.Single(tag => tag.Name == tagName).Color;

    // --- Choose a colour ---

    [Test]
    public void NewTagHasNoColourSoItShowsTheDefault()
    {
        Assert.That(colorOf(firstNoteId, "Work"), Is.Null);
    }

    [Test]
    public void UpdateTagColorChangesItOnEveryTodoWithTheTag()
    {
        controller.UpdateTag(workId, color: Blue);

        Assert.That(colorOf(firstNoteId, "Work"), Is.EqualTo(Blue));
        Assert.That(colorOf(secondNoteId, "Work"), Is.EqualTo(Blue));
    }

    [Test]
    public void UpdateTagColorLeavesOtherTagsAlone()
    {
        controller.UpdateTag(workId, color: Blue);

        Assert.That(colorOf(firstNoteId, "Home"), Is.Null);
    }

    [Test]
    public void UpdateTagColorIsStoredInUpperCaseAndTrimmed()
    {
        controller.UpdateTag(workId, color: " #0f6cbd ");

        Assert.That(colorOf(secondNoteId, "Work"), Is.EqualTo(Blue));
    }

    [Test]
    public void UpdateTagColorKeepsTheTagsIdAndName()
    {
        controller.UpdateTag(workId, color: Blue);

        Assert.That(tagId("Work"), Is.EqualTo(workId));
    }

    [TestCase("")]
    [TestCase("blue")]
    [TestCase("#FFF")]
    [TestCase("#GGGGGG")]
    [TestCase("0F6CBD")]
    public void UpdateTagColorRejectsInvalidColoursAndChangesNothing(string invalidColor)
    {
        Assert.Throws<ArgumentException>(() => controller.UpdateTag(workId, color: invalidColor));

        Assert.That(colorOf(firstNoteId, "Work"), Is.Null);
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void UpdateTagChangesNameAndColourTogether()
    {
        controller.UpdateTag(workId, name: "Job", color: Blue);

        Assert.That(note(firstNoteId).Tags.Single(tag => tag.Id == workId).Name, Is.EqualTo("Job"));
        Assert.That(colorOf(firstNoteId, "Job"), Is.EqualTo(Blue));
        Assert.That(recorder.Names, Is.EqualTo(new[]
        {
            nameof(BoardController.TagChanged),
            nameof(BoardController.BoardChanged)
        }));
    }

    [Test]
    public void UpdateTagWithInvalidColourKeepsTheOldName()
    {
        Assert.Throws<ArgumentException>(() => controller.UpdateTag(workId, name: "Job", color: "blue"));

        Assert.That(note(firstNoteId).Tags.Single(tag => tag.Id == workId).Name, Is.EqualTo("Work"));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void SetColorOfUnknownTagThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.UpdateTag(Guid.NewGuid(), color: Blue));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void UpdateTagColorRaisesTagChangedThenBoardChanged()
    {
        controller.UpdateTag(workId, color: Blue);

        Assert.That(recorder.Names, Is.EqualTo(new[]
        {
            nameof(BoardController.TagChanged),
            nameof(BoardController.BoardChanged)
        }));
        var changed = recorder.ArgsOf<TagChangedEventArgs>().Single();
        Assert.That(changed.TagId, Is.EqualTo(workId));
        Assert.That(changed.Name, Is.EqualTo("Work"));
        Assert.That(changed.Color, Is.EqualTo(Blue));
    }

    // --- Colour follows the tag ---

    [Test]
    public void TagAddedToAnotherTodoTakesItsExistingColour()
    {
        controller.UpdateTag(workId, color: Blue);

        controller.UpdateNote(thirdNoteId, new NoteChanges { TagNames = new[] { "work" } });

        Assert.That(colorOf(thirdNoteId, "Work"), Is.EqualTo(Blue));
    }

    [Test]
    public void TagOnANewTodoTakesItsExistingColour()
    {
        controller.UpdateTag(workId, color: Blue);

        Guid newNoteId = controller.CreateNote(plannedId, "New", "", tagNames: new[] { "Work" });

        Assert.That(colorOf(newNoteId, "Work"), Is.EqualTo(Blue));
    }

    [Test]
    public void RenamingATagKeepsItsColour()
    {
        controller.UpdateTag(workId, color: Blue);
        recorder.Clear();

        controller.UpdateTag(workId, name: "Job");

        Assert.That(colorOf(secondNoteId, "Job"), Is.EqualTo(Blue));
        Assert.That(recorder.ArgsOf<TagChangedEventArgs>().Single().Color, Is.EqualTo(Blue));
    }

    [Test]
    public void ColourIsGoneWithTheLastTodoThatHadTheTag()
    {
        controller.UpdateTag(homeId, color: Blue);
        controller.DeleteTag(homeId);

        controller.UpdateNote(thirdNoteId, new NoteChanges { TagNames = new[] { "Home" } });

        Assert.That(colorOf(thirdNoteId, "Home"), Is.Null);
    }

    // --- Colour chosen before the todo is saved ---

    [Test]
    public void NewTagOnANewTodoTakesTheChosenColour()
    {
        Guid noteId = controller.CreateNote(plannedId, "New", "", tagNames: new[] { "Garden" },
            newTagColors: new Dictionary<string, string> { ["garden"] = Blue });

        Assert.That(colorOf(noteId, "Garden"), Is.EqualTo(Blue));
    }

    [Test]
    public void NewTagAddedToAnExistingTodoTakesTheChosenColour()
    {
        controller.UpdateNote(thirdNoteId, new NoteChanges { TagNames = new[] { "Garden" }, NewTagColors = new Dictionary<string, string> { ["Garden"] = "#c62828" } });

        Assert.That(colorOf(thirdNoteId, "Garden"), Is.EqualTo("#C62828"));
    }

    [Test]
    public void ChosenColourIsIgnoredForATagThatIsAlreadyOnTheBoard()
    {
        controller.UpdateTag(workId, color: Blue);

        controller.UpdateNote(thirdNoteId, new NoteChanges { TagNames = new[] { "Work" }, NewTagColors = new Dictionary<string, string> { ["Work"] = "#C62828" } });

        Assert.That(colorOf(thirdNoteId, "Work"), Is.EqualTo(Blue));
    }

    [Test]
    public void InvalidChosenColourIsRejectedAndChangesNothing()
    {
        Assert.Throws<ArgumentException>(() => controller.UpdateNote(thirdNoteId, new NoteChanges { TagNames = new[] { "Garden" }, NewTagColors = new Dictionary<string, string> { ["Garden"] = "blue" } }));

        Assert.That(note(thirdNoteId).Tags, Is.Empty);
        Assert.That(recorder.Names, Is.Empty);
    }

    // --- Persistence ---

    [Test]
    public void ColourIsKeptWhenTheBoardIsSavedAndLoaded()
    {
        controller.UpdateTag(workId, color: Blue);
        controller.SaveBoardToJson(jsonPath);

        var reloaded = new LocalBoardController();
        reloaded.LoadBoardFromJson(jsonPath);

        List<NoteTag> work = reloaded.GetStatusColumnIds()
            .SelectMany(reloaded.GetNotesInColumn)
            .SelectMany(note => note.Tags)
            .Where(tag => tag.Name == "Work")
            .ToList();
        Assert.That(work, Has.Count.EqualTo(2));
        Assert.That(work.Select(tag => tag.Color), Is.All.EqualTo(Blue));
    }

    [Test]
    public void BoardSavedBeforeColoursExistedLoadsWithTheDefaultColour()
    {
        File.WriteAllText(jsonPath, """
            {
              "StatusColumns": [
                {
                  "Id": "11111111-1111-1111-1111-111111111111",
                  "Title": "Planned",
                  "Notes": [
                    {
                      "Id": "22222222-2222-2222-2222-222222222222",
                      "Title": "Old",
                      "Content": "",
                      "Tags": [ { "Id": "33333333-3333-3333-3333-333333333333", "Name": "Work" } ]
                    }
                  ]
                }
              ]
            }
            """);

        controller.LoadBoardFromJson(jsonPath);

        Assert.That(controller.GetTags().Single().Color, Is.Null);
    }

    [Test]
    public void LoadingMakesDifferingColoursOfTheSameTagConsistent()
    {
        controller.UpdateTag(workId, color: Blue);
        controller.SaveBoardToJson(jsonPath);
        string json = File.ReadAllText(jsonPath);
        // Make the second copy of the tag differ, as a hand-edited file could
        int secondCopy = json.LastIndexOf(Blue, StringComparison.Ordinal);
        File.WriteAllText(jsonPath, json.Remove(secondCopy, Blue.Length).Insert(secondCopy, "#C62828"));

        controller.LoadBoardFromJson(jsonPath);

        Assert.That(colorOf(firstNoteId, "Work"), Is.EqualTo(colorOf(secondNoteId, "Work")));
    }
}
