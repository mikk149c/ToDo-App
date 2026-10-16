namespace ToDo_App.Tests;

using ToDo_App.Controller;
using ToDo_App.Interfaces;

// Tags on todos (#40)
public class TagTests
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
        secondNoteId = controller.CreateNote(progressId, "Second note", "Australia");
        recorder = new EventRecorder(controller);
    }

    private ToDoNote note(Guid noteId) =>
        controller.GetStatusColumnIds().SelectMany(controller.GetNotesInColumn).Single(note => note.Id == noteId);

    private List<string> tagNames(Guid noteId) => note(noteId).Tags.Select(tag => tag.Name).ToList();

    // --- Tag names ---

    [TestCase("Work", "Work")]
    [TestCase("  Home  ", "Home")]
    [TestCase("TwelveChars!", "TwelveChars!")]
    public void NormalizeNameTrimsValidNames(string name, string expected)
    {
        Assert.That(NoteTag.NormalizeName(name), Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    [TestCase("ThirteenChars")]
    public void NormalizeNameRejectsEmptyOrTooLongNames(string? name)
    {
        Assert.Throws<ArgumentException>(() => NoteTag.NormalizeName(name));
    }

    [Test]
    public void NamesMatchIgnoresCase()
    {
        Assert.That(NoteTag.NamesMatch("work", "WORK"), Is.True);
        Assert.That(NoteTag.NamesMatch("work", "home"), Is.False);
    }

    [Test]
    public void NewTagsGetUniqueIds()
    {
        var first = new NoteTag("Work");
        var second = new NoteTag("Work");

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public void NewNoteHasNoTags()
    {
        Assert.That(new ToDoNote("Title", "Content").Tags, Is.Empty);
    }

    // --- Adding and removing ---

    [Test]
    public void SetNoteTagsAddsTagsInOrder()
    {
        controller.SetNoteTags(firstNoteId, new[] { "Work", "Urgent" });

        Assert.That(tagNames(firstNoteId), Is.EqualTo(new[] { "Work", "Urgent" }));
    }

    [Test]
    public void SetNoteTagsTrimsNames()
    {
        controller.SetNoteTags(firstNoteId, new[] { "  Home " });

        Assert.That(tagNames(firstNoteId), Is.EqualTo(new[] { "Home" }));
    }

    [Test]
    public void SetNoteTagsHasNoLimitOnNumberOfTags()
    {
        var names = Enumerable.Range(1, 50).Select(i => $"Tag {i}").ToList();

        controller.SetNoteTags(firstNoteId, names);

        Assert.That(tagNames(firstNoteId), Is.EqualTo(names));
    }

    [Test]
    public void SetNoteTagsWithEmptyListRemovesAllTags()
    {
        controller.SetNoteTags(firstNoteId, new[] { "Work" });

        controller.SetNoteTags(firstNoteId, Array.Empty<string>());

        Assert.That(note(firstNoteId).Tags, Is.Empty);
    }

    [Test]
    public void RemovingTagFromOneNoteKeepsItOnOthers()
    {
        controller.SetNoteTags(firstNoteId, new[] { "Urgent" });
        controller.SetNoteTags(secondNoteId, new[] { "Urgent" });

        controller.SetNoteTags(firstNoteId, Array.Empty<string>());

        Assert.That(tagNames(secondNoteId), Is.EqualTo(new[] { "Urgent" }));
    }

    [Test]
    public void SameTagTwiceOnANoteIsKeptOnce()
    {
        controller.SetNoteTags(firstNoteId, new[] { "Work", "WORK", "work " });

        Assert.That(tagNames(firstNoteId), Is.EqualTo(new[] { "Work" }));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("ThirteenChars")]
    public void InvalidTagNameThrowsAndLeavesTagsAndEventsUnchanged(string invalidName)
    {
        controller.SetNoteTags(firstNoteId, new[] { "Work" });
        recorder.Clear();

        Assert.Throws<ArgumentException>(() => controller.SetNoteTags(firstNoteId, new[] { "Home", invalidName }));

        Assert.That(tagNames(firstNoteId), Is.EqualTo(new[] { "Work" }));
        Assert.That(recorder.Names, Is.Empty);
    }

    [Test]
    public void SetNoteTagsWithUnknownNoteThrowsWithoutEvents()
    {
        Assert.Throws<KeyNotFoundException>(() => controller.SetNoteTags(Guid.NewGuid(), new[] { "Work" }));
        Assert.That(recorder.Names, Is.Empty);
    }

    // --- Tag identity and the derived tag list ---

    [Test]
    public void AddingExistingTagNameReusesItsIdAndSpelling()
    {
        controller.SetNoteTags(secondNoteId, new[] { "Work" });

        controller.SetNoteTags(firstNoteId, new[] { "work" });

        NoteTag reused = note(firstNoteId).Tags.Single();
        NoteTag original = note(secondNoteId).Tags.Single();
        Assert.That(reused.Id, Is.EqualTo(original.Id));
        Assert.That(reused.Name, Is.EqualTo("Work"));
    }

    [Test]
    public void SettingSameTagsAgainKeepsTagIds()
    {
        controller.SetNoteTags(firstNoteId, new[] { "Work" });
        Guid tagId = note(firstNoteId).Tags.Single().Id;

        controller.SetNoteTags(firstNoteId, new[] { "Work", "Home" });

        Assert.That(note(firstNoteId).Tags[0].Id, Is.EqualTo(tagId));
    }

    [Test]
    public void NotesShareTagIdButNotTheTagObject()
    {
        controller.SetNoteTags(firstNoteId, new[] { "Work" });
        controller.SetNoteTags(secondNoteId, new[] { "Work" });

        // Each note stores its own copy, the same way they are stored in board.json
        Assert.That(note(firstNoteId).Tags.Single(), Is.Not.SameAs(note(secondNoteId).Tags.Single()));
    }

    [Test]
    public void GetTagsListsEachTagOnceSortedByName()
    {
        controller.SetNoteTags(firstNoteId, new[] { "work", "Home" });
        controller.SetNoteTags(secondNoteId, new[] { "Work", "Bug" });

        Assert.That(controller.GetTags().Select(tag => tag.Name), Is.EqualTo(new[] { "Bug", "Home", "work" }));
    }

    [Test]
    public void TagDisappearsWhenItsLastNoteIsDeleted()
    {
        controller.SetNoteTags(firstNoteId, new[] { "Garden", "Work" });
        controller.SetNoteTags(secondNoteId, new[] { "Work" });

        controller.RemoveNote(firstNoteId);

        Assert.That(controller.GetTags().Select(tag => tag.Name), Is.EqualTo(new[] { "Work" }));
    }

    [Test]
    public void TagDisappearsWhenRemovedFromItsLastNote()
    {
        controller.SetNoteTags(firstNoteId, new[] { "Garden" });

        controller.SetNoteTags(firstNoteId, Array.Empty<string>());

        Assert.That(controller.GetTags(), Is.Empty);
    }

    [Test]
    public void TagCreatedAgainAfterItDisappearedIsANewTag()
    {
        controller.SetNoteTags(firstNoteId, new[] { "Garden" });
        Guid oldId = note(firstNoteId).Tags.Single().Id;
        controller.SetNoteTags(firstNoteId, Array.Empty<string>());

        controller.SetNoteTags(secondNoteId, new[] { "Garden" });

        Assert.That(note(secondNoteId).Tags.Single().Id, Is.Not.EqualTo(oldId));
    }

    [Test]
    public void TagsAreKeptWhenNoteMoves()
    {
        controller.SetNoteTags(firstNoteId, new[] { "Work" });
        Guid tagId = note(firstNoteId).Tags.Single().Id;

        controller.MoveNoteToColumn(firstNoteId, progressId, 0);

        Assert.That(note(firstNoteId).Tags.Single().Id, Is.EqualTo(tagId));
    }

    // --- Creating a note with tags ---

    [Test]
    public void CreateNoteWithTagsStoresThem()
    {
        controller.SetNoteTags(secondNoteId, new[] { "Work" });

        Guid noteId = controller.CreateNote(plannedId, "New", "Content", tagNames: new[] { "work", "Home" });

        Assert.That(tagNames(noteId), Is.EqualTo(new[] { "Work", "Home" }));
        Assert.That(note(noteId).Tags[0].Id, Is.EqualTo(note(secondNoteId).Tags.Single().Id));
    }

    [Test]
    public void CreateNoteWithInvalidTagCreatesNothing()
    {
        Assert.Throws<ArgumentException>(() =>
            controller.CreateNote(plannedId, "New", "Content", tagNames: new[] { "ThirteenChars" }));

        Assert.That(controller.GetNotesInColumn(plannedId), Has.Count.EqualTo(1));
        Assert.That(recorder.Names, Is.Empty);
    }

    // --- Events ---

    [Test]
    public void SetNoteTagsRaisesContentChangedForItsColumnThenBoardChanged()
    {
        controller.SetNoteTags(secondNoteId, new[] { "Work" });

        Assert.That(recorder.Names, Is.EqualTo(new[]
        {
            nameof(BoardController.ColumnContentChanged),
            nameof(BoardController.BoardChanged)
        }));
        var changed = recorder.ArgsOf<ColumnContentChangedEventArgs>().Single();
        Assert.That(changed.ColumnId, Is.EqualTo(progressId));
        Assert.That(changed.NewNotes.Single().Tags.Single().Name, Is.EqualTo("Work"));
    }
}

// Tags in board.json (#40)
public class TagPersistenceTests
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

    private static List<ToDoNote> allNotes(BoardController controller) =>
        controller.GetStatusColumnIds().SelectMany(controller.GetNotesInColumn).ToList();

    [Test]
    public void RoundTripKeepsTagIdsAndNames()
    {
        var original = new LocalBoardController();
        Guid columnId = original.CreateColumn("Planned");
        Guid firstId = original.CreateNote(columnId, "First", "", tagNames: new[] { "Work", "Home" });
        Guid secondId = original.CreateNote(columnId, "Second", "", tagNames: new[] { "Work" });
        original.SaveBoardToJson(jsonPath);

        var loaded = new LocalBoardController();
        loaded.LoadBoardFromJson(jsonPath);

        List<ToDoNote> originalNotes = allNotes(original);
        List<ToDoNote> loadedNotes = allNotes(loaded);
        for (int i = 0; i < originalNotes.Count; i++)
        {
            Assert.That(loadedNotes[i].Tags.Select(tag => tag.Id), Is.EqualTo(originalNotes[i].Tags.Select(tag => tag.Id)));
            Assert.That(loadedNotes[i].Tags.Select(tag => tag.Name), Is.EqualTo(originalNotes[i].Tags.Select(tag => tag.Name)));
        }
        Assert.That(loadedNotes.Single(n => n.Id == secondId).Tags.Single().Id,
            Is.EqualTo(loadedNotes.Single(n => n.Id == firstId).Tags[0].Id));
    }

    [Test]
    public void BoardSavedBeforeTagsExistedLoadsWithoutTags()
    {
        File.WriteAllText(jsonPath, """
            {
              "StatusColumns": [
                {
                  "Id": "11111111-1111-1111-1111-111111111111",
                  "Title": "Planned",
                  "Notes": [
                    { "Id": "22222222-2222-2222-2222-222222222222", "Title": "Old note", "Content": "No tags here" }
                  ]
                }
              ]
            }
            """);
        var controller = new LocalBoardController();

        controller.LoadBoardFromJson(jsonPath);

        ToDoNote loadedNote = allNotes(controller).Single();
        Assert.That(loadedNote.Title, Is.EqualTo("Old note"));
        Assert.That(loadedNote.Tags, Is.Empty);
        Assert.That(controller.GetTags(), Is.Empty);
    }

    [Test]
    public void LoadingNullTagsGivesEmptyTagList()
    {
        File.WriteAllText(jsonPath, """
            { "StatusColumns": [ { "Id": "11111111-1111-1111-1111-111111111111", "Title": "Planned",
              "Notes": [ { "Id": "22222222-2222-2222-2222-222222222222", "Title": "Note", "Content": "", "Tags": null } ] } ] }
            """);
        var controller = new LocalBoardController();

        controller.LoadBoardFromJson(jsonPath);

        Assert.That(allNotes(controller).Single().Tags, Is.Not.Null.And.Empty);
    }

    // Decision (#40 assumption): when copies of a tag disagree on the name, the first one found wins
    [Test]
    public void LoadingConflictingTagNamesKeepsFirstNameFound()
    {
        File.WriteAllText(jsonPath, """
            { "StatusColumns": [ { "Id": "11111111-1111-1111-1111-111111111111", "Title": "Planned", "Notes": [
              { "Id": "22222222-2222-2222-2222-222222222222", "Title": "First", "Content": "",
                "Tags": [ { "Id": "33333333-3333-3333-3333-333333333333", "Name": "Work" } ] },
              { "Id": "44444444-4444-4444-4444-444444444444", "Title": "Second", "Content": "",
                "Tags": [ { "Id": "33333333-3333-3333-3333-333333333333", "Name": "Job" } ] } ] } ] }
            """);
        var controller = new LocalBoardController();

        controller.LoadBoardFromJson(jsonPath);

        Assert.That(allNotes(controller).Select(n => n.Tags.Single().Name), Is.EqualTo(new[] { "Work", "Work" }));
        Assert.That(controller.GetTags(), Has.Count.EqualTo(1));
    }

    [Test]
    public void LoadingSameTagTwiceOnANoteKeepsItOnce()
    {
        File.WriteAllText(jsonPath, """
            { "StatusColumns": [ { "Id": "11111111-1111-1111-1111-111111111111", "Title": "Planned", "Notes": [
              { "Id": "22222222-2222-2222-2222-222222222222", "Title": "Note", "Content": "",
                "Tags": [ { "Id": "33333333-3333-3333-3333-333333333333", "Name": "Work" },
                          { "Id": "33333333-3333-3333-3333-333333333333", "Name": "Work" } ] } ] } ] }
            """);
        var controller = new LocalBoardController();

        controller.LoadBoardFromJson(jsonPath);

        Assert.That(allNotes(controller).Single().Tags, Has.Count.EqualTo(1));
    }
}
